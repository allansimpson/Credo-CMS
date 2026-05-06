import { useEffect, useRef, useState } from "react";
import { imagesApi, type ImageUploadResult } from "@/lib/api/images";

export interface ImageUploadValue {
  url: string | null;
  webpUrl: string | null;
  alt: string | null;
}

export interface ImageUploadProps {
  value: ImageUploadValue;
  onChange: (next: ImageUploadValue) => void;
  /** Optional caption shown under the field. */
  hint?: string;
  /** Width to render the preview at (max). Default 240px. */
  previewWidth?: number;
  /** Disable the input entirely. */
  disabled?: boolean;
  ariaLabel?: string;
}

/**
 * Image picker for admin forms. Accepts drag-drop or click. Uploads
 * synchronously to /api/admin/images/upload and returns the optimized
 * + WebP URLs via onChange. Stores alt text alongside the URL so a single
 * `value` object survives form round-trips.
 */
export function ImageUpload({
  value,
  onChange,
  hint,
  previewWidth = 240,
  disabled,
  ariaLabel,
}: ImageUploadProps) {
  const inputRef = useRef<HTMLInputElement | null>(null);
  const dropRef = useRef<HTMLDivElement | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [meta, setMeta] = useState<ImageUploadResult | null>(null);

  // Keep meta cleared if the parent resets `value.url` externally.
  useEffect(() => {
    if (!value.url) setMeta(null);
  }, [value.url]);

  const handleFile = async (file: File) => {
    setError(null);
    if (!file.type.startsWith("image/")) {
      setError("File must be an image (JPEG, PNG, or WebP).");
      return;
    }
    setUploading(true);
    try {
      const result = await imagesApi.upload(file);
      setMeta(result);
      onChange({
        url: result.blobUrl,
        webpUrl: result.webpBlobUrl,
        alt: value.alt,
      });
    } catch (err) {
      const messages =
        typeof err === "object" && err !== null && "getMessages" in err
          ? (err as { getMessages: () => string[] }).getMessages()
          : ["Upload failed."];
      setError(messages[0] ?? "Upload failed.");
    } finally {
      setUploading(false);
    }
  };

  const handleClear = () => {
    setMeta(null);
    setError(null);
    onChange({ url: null, webpUrl: null, alt: null });
    if (inputRef.current) inputRef.current.value = "";
  };

  return (
    <div>
      <div
        ref={dropRef}
        onDragOver={(e) => { e.preventDefault(); }}
        onDrop={(e) => {
          e.preventDefault();
          if (disabled || uploading) return;
          const file = e.dataTransfer.files?.[0];
          if (file) void handleFile(file);
        }}
        onClick={() => !disabled && !uploading && inputRef.current?.click()}
        role="button"
        tabIndex={0}
        aria-label={ariaLabel ?? "Image upload"}
        onKeyDown={(e) => {
          if ((e.key === "Enter" || e.key === " ") && !disabled && !uploading) {
            e.preventDefault();
            inputRef.current?.click();
          }
        }}
        className={
          "flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border border-dashed bg-background p-4 text-center text-sm transition-colors hover:bg-panel-alt/50 " +
          (uploading ? "opacity-60 cursor-progress " : "") +
          (disabled ? "opacity-50 cursor-not-allowed " : "")
        }
        style={{ minHeight: 120 }}
      >
        {value.url ? (
          <picture>
            {value.webpUrl && <source srcSet={value.webpUrl} type="image/webp" />}
            <img
              src={value.url}
              alt={value.alt ?? ""}
              style={{ maxWidth: previewWidth, maxHeight: previewWidth, height: "auto" }}
              className="rounded"
            />
          </picture>
        ) : (
          <div className="text-muted">
            <p>{uploading ? "Uploading…" : "Click or drop an image here"}</p>
            <p className="mt-1 text-xs">JPEG, PNG, or WebP</p>
          </div>
        )}

        <input
          ref={inputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          className="sr-only"
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) void handleFile(file);
          }}
          disabled={disabled || uploading}
        />
      </div>

      {value.url && (
        <div className="mt-2 grid gap-2 sm:grid-cols-2">
          <label className="block text-sm">
            <span className="mb-1 block font-medium">Alt text</span>
            <input
              value={value.alt ?? ""}
              onChange={(e) => onChange({ ...value, alt: e.target.value || null })}
              placeholder="Describe the image for screen readers"
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            />
          </label>
          <div className="flex items-end justify-end">
            <button
              type="button"
              onClick={handleClear}
              disabled={uploading || disabled}
              className="inline-flex h-9 items-center justify-center rounded-md border bg-card px-3 text-sm hover:bg-panel-alt disabled:opacity-50"
            >
              Remove
            </button>
          </div>
        </div>
      )}

      {meta && (
        <p className="mt-1 text-xs text-muted">
          {meta.width}×{meta.height}px • {Math.round(meta.sizeBytes / 1024)} KB
        </p>
      )}

      {hint && !error && (
        <p className="mt-1 text-xs text-muted">{hint}</p>
      )}

      {error && (
        <p role="alert" className="mt-1 text-xs text-danger">{error}</p>
      )}
    </div>
  );
}
