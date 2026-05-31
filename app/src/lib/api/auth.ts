import { apiGet, apiPost } from "@/lib/apiClient";
import type {
  AcceptInvitationRequest,
  ChangePasswordRequest,
  CurrentUser,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResult,
  ResetPasswordRequest,
} from "@/types/api";

/**
 * Tagged status of an invitation token. Mirrors the C# enum
 * <c>CredoCms.Application.Auth.InvitationPreviewStatus</c>. Backend uses
 * <c>JsonStringEnumConverter</c>, so the wire shape is a STRING — not a
 * number. Don't add numeric mappings.
 */
export type InvitationPreviewStatus = "Valid" | "Expired" | "Consumed" | "Invalid";

export interface InvitationPreview {
  status: InvitationPreviewStatus;
  firstName: string | null;
  lastName: string | null;
  email: string | null;
  role: string | null;
  invitedBy: string | null;
  churchName: string | null;
  churchInitials: string | null;
  credentialNumber: string | null;
  expiresAt: string | null; // ISO-8601 UTC, null unless status === "Valid"
}

export const authApi = {
  login: (req: LoginRequest) =>
    apiPost<LoginResult>("/api/auth/login", req, { emitUnauthorized: false }),

  logout: () => apiPost<void>("/api/auth/logout"),

  me: () =>
    apiGet<CurrentUser>("/api/auth/me", { emitUnauthorized: false }).catch(
      // /me legitimately returns 401 when anonymous; convert that to null so the
      // auth context can decide whether to redirect.
      (err) => {
        if (
          typeof err === "object" &&
          err !== null &&
          "status" in err &&
          (err as { status?: number }).status === 401
        ) {
          return null;
        }
        throw err;
      },
    ),

  forgotPassword: (req: ForgotPasswordRequest) =>
    apiPost<{ ok: boolean }>("/api/auth/forgot-password", req, {
      emitUnauthorized: false,
    }),

  resetPassword: (req: ResetPasswordRequest) =>
    apiPost<{ ok: boolean }>("/api/auth/reset-password", req, {
      emitUnauthorized: false,
    }),

  acceptInvitation: (req: AcceptInvitationRequest) =>
    apiPost<{ ok: boolean }>("/api/auth/accept-invitation", req, {
      emitUnauthorized: false,
    }),

  invitationPreview: (email: string, token: string) => {
    const qs = new URLSearchParams({ email, token }).toString();
    return apiGet<InvitationPreview>(`/api/auth/invitation-preview?${qs}`, {
      emitUnauthorized: false,
    });
  },

  changePassword: (req: ChangePasswordRequest) =>
    apiPost<{ ok: boolean }>("/api/auth/change-password", req),
};
