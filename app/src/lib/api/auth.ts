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

  changePassword: (req: ChangePasswordRequest) =>
    apiPost<{ ok: boolean }>("/api/auth/change-password", req),
};
