import type { ApiErrorResponse } from "@/types/api";

/**
 * Hand-written API client. Wraps fetch with sensible defaults and emits a
 * window event on 401 responses so the auth context can react to session
 * expiry without each call having to handle it manually.
 */

const SESSION_EXPIRY_HEADER = "x-session-expires-at";

export const ApiEvents = {
  /** A 401 was returned by an authenticated endpoint. */
  Unauthorized: "credocms:unauthorized",
  /** A response carried X-Session-Expires-At — value is an ISO timestamp string. */
  SessionExpiryHeader: "credocms:session-expires-at",
} as const;

export class ApiError extends Error {
  status: number;
  body: ApiErrorResponse | unknown;

  constructor(status: number, body: ApiErrorResponse | unknown, message?: string) {
    super(message ?? `Request failed with status ${status}`);
    this.status = status;
    this.body = body;
  }

  /** Returns a map of fieldName → error messages from FluentValidation responses. */
  getFieldErrors(): Record<string, string[]> {
    if (this.body && typeof this.body === "object" && "errors" in this.body) {
      const errors = (this.body as { errors: unknown }).errors;
      if (errors && typeof errors === "object" && !Array.isArray(errors)) {
        const result: Record<string, string[]> = {};
        for (const [field, messages] of Object.entries(errors as Record<string, unknown>)) {
          if (Array.isArray(messages)) {
            result[field] = messages.filter((m): m is string => typeof m === "string");
          } else if (typeof messages === "string") {
            result[field] = [messages];
          }
        }
        return result;
      }
    }
    return {};
  }

  /** Returns user-facing error messages from a typical API error envelope. */
  getMessages(): string[] {
    if (this.body && typeof this.body === "object" && "errors" in this.body) {
      const errors = (this.body as { errors: unknown }).errors;
      // Service-layer shape: { errors: string[] }
      if (Array.isArray(errors)) return errors as string[];
      // ASP.NET ProblemDetails / FluentValidation auto-validation shape:
      // { errors: { fieldName: ["message", ...] } }
      if (errors && typeof errors === "object") {
        const flat: string[] = [];
        for (const messages of Object.values(errors as Record<string, unknown>)) {
          if (Array.isArray(messages)) {
            for (const m of messages) if (typeof m === "string") flat.push(m);
          } else if (typeof messages === "string") {
            flat.push(messages);
          }
        }
        if (flat.length > 0) return flat;
      }
    }
    return [this.message];
  }
}

interface RequestOptions {
  method?: string;
  body?: unknown;
  signal?: AbortSignal;
  /** When false, do not emit Unauthorized event on 401. */
  emitUnauthorized?: boolean;
}

async function request<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {
    Accept: "application/json",
  };

  let body: BodyInit | undefined;
  if (opts.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(opts.body);
  }

  const response = await fetch(path, {
    method: opts.method ?? "GET",
    headers,
    body,
    credentials: "include",
    signal: opts.signal,
  });

  // Surface the session-expires-at header to anyone listening (the auth context).
  const expiresAt = response.headers.get(SESSION_EXPIRY_HEADER);
  if (expiresAt) {
    window.dispatchEvent(
      new CustomEvent(ApiEvents.SessionExpiryHeader, { detail: expiresAt }),
    );
  }

  if (response.status === 401 && opts.emitUnauthorized !== false) {
    window.dispatchEvent(new CustomEvent(ApiEvents.Unauthorized));
  }

  if (response.status === 204) {
    return undefined as T;
  }

  let parsed: unknown = null;
  const contentType = response.headers.get("content-type") ?? "";
  // Match application/json *and* RFC 6838 structured-suffix JSON types like
  // application/problem+json (ASP.NET ProblemDetails) or application/ld+json.
  if (contentType.includes("application/json") || contentType.includes("+json")) {
    parsed = await response.json().catch(() => null);
  } else if (response.body) {
    parsed = await response.text().catch(() => null);
  }

  if (!response.ok) {
    throw new ApiError(response.status, parsed);
  }

  return parsed as T;
}

export const apiGet = <T>(path: string, opts?: RequestOptions) =>
  request<T>(path, { ...opts, method: "GET" });
export const apiPost = <T>(path: string, body?: unknown, opts?: RequestOptions) =>
  request<T>(path, { ...opts, method: "POST", body });
export const apiPut = <T>(path: string, body?: unknown, opts?: RequestOptions) =>
  request<T>(path, { ...opts, method: "PUT", body });
export const apiDelete = <T>(path: string, body?: unknown, opts?: RequestOptions) =>
  request<T>(path, { ...opts, method: "DELETE", body });

/**
 * Multipart upload. Uses the same fetch shape as `request` but lets the
 * browser set the Content-Type header itself (so it includes the
 * multipart boundary).
 */
export async function apiUpload<T>(path: string, formData: FormData): Promise<T> {
  const response = await fetch(path, {
    method: "POST",
    body: formData,
    credentials: "include",
    headers: { Accept: "application/json" },
  });

  const expiresAt = response.headers.get(SESSION_EXPIRY_HEADER);
  if (expiresAt) {
    window.dispatchEvent(
      new CustomEvent(ApiEvents.SessionExpiryHeader, { detail: expiresAt }),
    );
  }
  if (response.status === 401) {
    window.dispatchEvent(new CustomEvent(ApiEvents.Unauthorized));
  }
  if (response.status === 204) return undefined as T;

  let parsed: unknown = null;
  const contentType = response.headers.get("content-type") ?? "";
  // Match application/json *and* RFC 6838 structured-suffix JSON types like
  // application/problem+json (ASP.NET ProblemDetails) or application/ld+json.
  if (contentType.includes("application/json") || contentType.includes("+json")) {
    parsed = await response.json().catch(() => null);
  } else if (response.body) {
    parsed = await response.text().catch(() => null);
  }

  if (!response.ok) {
    throw new ApiError(response.status, parsed);
  }
  return parsed as T;
}
