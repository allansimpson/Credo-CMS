import { apiGet, apiPut } from "@/lib/apiClient";

export interface Profile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  phoneNumber: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  stateOrRegion: string | null;
  postalCode: string | null;
  country: string | null;
  photoBlobUrl: string | null;
  photoWebpBlobUrl: string | null;
  photoAltText: string | null;
  publicAuthorBio: string | null;
  isListedInDirectory: boolean;
  showEmailInDirectory: boolean;
  showPhoneInDirectory: boolean;
  showAddressInDirectory: boolean;
  showPhotoInDirectory: boolean;
  receiveNewsEmails: boolean;
  receiveBlogEmails: boolean;
  receiveBroadcastEmails: boolean;
  receiveGroupEmailsGlobal: boolean;
}

export interface UpdatePersonalInfoRequest {
  phoneNumber: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  stateOrRegion: string | null;
  postalCode: string | null;
  country: string | null;
  photoBlobUrl: string | null;
  photoWebpBlobUrl: string | null;
  photoAltText: string | null;
  publicAuthorBio: string | null;
}

export interface UpdateDirectoryRequest {
  isListedInDirectory: boolean;
  showEmailInDirectory: boolean;
  showPhoneInDirectory: boolean;
  showAddressInDirectory: boolean;
  showPhotoInDirectory: boolean;
}

export interface UpdateNotificationsRequest {
  receiveNewsEmails: boolean;
  receiveBlogEmails: boolean;
  receiveBroadcastEmails: boolean;
  receiveGroupEmailsGlobal: boolean;
}

export const profileApi = {
  get: () => apiGet<Profile>("/api/profile"),
  updatePersonal: (req: UpdatePersonalInfoRequest) =>
    apiPut<Profile>("/api/profile/personal", req),
  updateDirectory: (req: UpdateDirectoryRequest) =>
    apiPut<Profile>("/api/profile/directory", req),
  updateNotifications: (req: UpdateNotificationsRequest) =>
    apiPut<Profile>("/api/profile/notifications", req),
};

