import { apiUpload } from "@/lib/apiClient";

export interface ImageUploadResult {
  blobUrl: string;
  webpBlobUrl: string;
  width: number;
  height: number;
  sizeBytes: number;
}

export const imagesApi = {
  upload: (file: File): Promise<ImageUploadResult> => {
    const form = new FormData();
    form.append("file", file);
    return apiUpload<ImageUploadResult>("/api/admin/images/upload", form);
  },
};
