import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/apiClient";

export type EventRegistrationFieldType = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;

export interface EventRegistrationField {
  id: string;
  displayOrder: number;
  label: string;
  fieldType: EventRegistrationFieldType;
  required: boolean;
  helpText: string | null;
  options: string[] | null;
  textMaxLength: number | null;
  numberMin: number | null;
  numberMax: number | null;
}

export interface EventRegistration {
  id: string;
  eventId: string;
  occurrenceDate: string | null;
  submitterName: string;
  submitterEmail: string;
  submitterPhone: string | null;
  status: 0 | 1 | 2;
  submittedAt: string;
  canceledAt: string | null;
  cancelReason: string | null;
  fieldValues: Record<string, unknown>;
}

export interface SubmitRegistrationRequest {
  occurrenceDate: string | null;
  submitterName: string;
  submitterEmail: string;
  submitterPhone: string | null;
  fieldValues: Record<string, unknown>;
  hp: string | null;
  formOpenedElapsedMs: number;
}

export interface SubmitRegistrationResponse {
  registration: EventRegistration;
  cancelToken: string;
}

export const eventRegistrationApi = {
  listPublicFields: (slug: string) =>
    apiGet<EventRegistrationField[]>(`/api/public/events/${encodeURIComponent(slug)}/registration-fields`,
      { emitUnauthorized: false }),

  submit: (slug: string, body: SubmitRegistrationRequest) =>
    apiPost<SubmitRegistrationResponse>(`/api/public/events/${encodeURIComponent(slug)}/register`, body,
      { emitUnauthorized: false }),

  validateCancelToken: (slug: string, token: string) =>
    apiGet<{ ok: boolean }>(`/api/public/events/${encodeURIComponent(slug)}/register/cancel?token=${encodeURIComponent(token)}`,
      { emitUnauthorized: false }),

  cancel: (slug: string, token: string, reason?: string) =>
    apiPost<void>(`/api/public/events/${encodeURIComponent(slug)}/register/cancel`,
      { token, reason }, { emitUnauthorized: false }),

  // Admin
  listFields: (eventId: string) =>
    apiGet<EventRegistrationField[]>(`/api/admin/events/${eventId}/registration-fields`),
  addField: (eventId: string, body: CreateRegistrationFieldRequest) =>
    apiPost<EventRegistrationField>(`/api/admin/events/${eventId}/registration-fields`, body),
  updateField: (eventId: string, fieldId: string, body: CreateRegistrationFieldRequest) =>
    apiPut<EventRegistrationField>(`/api/admin/events/${eventId}/registration-fields/${fieldId}`, body),
  removeField: (eventId: string, fieldId: string) =>
    apiDelete<void>(`/api/admin/events/${eventId}/registration-fields/${fieldId}`),

  listRegistrations: (eventId: string, status?: number) => {
    const q = status !== undefined ? `?status=${status}` : "";
    return apiGet<EventRegistration[]>(`/api/admin/events/${eventId}/registrations${q}`);
  },
  cancelRegistration: (eventId: string, regId: string, reason?: string) =>
    apiPost<void>(`/api/admin/events/${eventId}/registrations/${regId}/cancel`, { reason }),
  resendConfirmation: (eventId: string, regId: string) =>
    apiPost<void>(`/api/admin/events/${eventId}/registrations/${regId}/resend-confirmation`),
  csvExportUrl: (eventId: string) =>
    `/api/admin/events/${eventId}/registrations/export.csv`,
};

export interface CreateRegistrationFieldRequest {
  label: string;
  fieldType: EventRegistrationFieldType;
  required: boolean;
  helpText: string | null;
  options: string[] | null;
  textMaxLength: number | null;
  numberMin: number | null;
  numberMax: number | null;
  displayOrder: number;
}
