import { useEffect, useState } from "react";
import { getBookInfo, NEW_TESTAMENT, OLD_TESTAMENT } from "@/lib/bible/books";
import {
  formatScriptureReference,
  validateScriptureReference,
  type ScriptureReference,
} from "@/lib/bible/scripture";

export interface ScriptureReferenceInputProps {
  value: ScriptureReference;
  onChange: (next: ScriptureReference) => void;
  onRemove?: () => void;
  ariaLabel?: string;
}

export function ScriptureReferenceInput({
  value, onChange, onRemove, ariaLabel,
}: ScriptureReferenceInputProps) {
  const [showRange, setShowRange] = useState(
    value.chapterEnd !== null || value.verseEnd !== null,
  );
  const [validation, setValidation] = useState<string | null>(null);

  useEffect(() => {
    setValidation(validateScriptureReference(value));
  }, [value]);

  const info = getBookInfo(value.book);

  const update = (patch: Partial<ScriptureReference>) => onChange({ ...value, ...patch });

  return (
    <div role="group" aria-label={ariaLabel ?? "Scripture reference"}
      className="space-y-2 rounded-md border bg-card p-3">
      <div className="grid grid-cols-1 gap-2 sm:grid-cols-[1fr_auto_auto]">
        <label className="block text-sm">
          <span className="mb-1 block font-medium">Book</span>
          <select
            value={value.book}
            onChange={(e) => update({ book: Number(e.target.value) })}
            className="h-10 w-full rounded-md border bg-background px-3 text-sm"
          >
            <optgroup label="Old Testament">
              {OLD_TESTAMENT.map((b) => (
                <option key={b.book} value={b.book}>{b.name}</option>
              ))}
            </optgroup>
            <optgroup label="New Testament">
              {NEW_TESTAMENT.map((b) => (
                <option key={b.book} value={b.book}>{b.name}</option>
              ))}
            </optgroup>
          </select>
        </label>
        <label className="block text-sm">
          <span className="mb-1 block font-medium">Chapter</span>
          <input
            type="number" min={1} max={info?.chapterCount ?? 150}
            value={value.chapterStart}
            onChange={(e) => update({ chapterStart: Number(e.target.value) })}
            className="h-10 w-24 rounded-md border bg-background px-3 text-sm"
          />
        </label>
        <label className="block text-sm">
          <span className="mb-1 block font-medium">Verse <span className="text-muted">(optional)</span></span>
          <input
            type="number" min={1}
            value={value.verseStart ?? ""}
            onChange={(e) => update({ verseStart: e.target.value === "" ? null : Number(e.target.value) })}
            className="h-10 w-24 rounded-md border bg-background px-3 text-sm"
          />
        </label>
      </div>

      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={showRange}
          onChange={(e) => {
            setShowRange(e.target.checked);
            if (!e.target.checked) update({ chapterEnd: null, verseEnd: null });
          }}
        />
        Through (extends to end chapter / verse)
      </label>

      {showRange && (
        <div className="grid grid-cols-2 gap-2">
          <label className="block text-sm">
            <span className="mb-1 block font-medium">End chapter</span>
            <input
              type="number" min={value.chapterStart} max={info?.chapterCount ?? 150}
              value={value.chapterEnd ?? value.chapterStart}
              onChange={(e) => update({ chapterEnd: Number(e.target.value) })}
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            />
          </label>
          <label className="block text-sm">
            <span className="mb-1 block font-medium">End verse <span className="text-muted">(optional)</span></span>
            <input
              type="number" min={1}
              value={value.verseEnd ?? ""}
              onChange={(e) => update({ verseEnd: e.target.value === "" ? null : Number(e.target.value) })}
              className="h-10 w-full rounded-md border bg-background px-3 text-sm"
            />
          </label>
        </div>
      )}

      <div className="flex items-center justify-between text-xs">
        <span className="text-muted">{formatScriptureReference(value)}</span>
        {onRemove && (
          <button type="button" onClick={onRemove}
            className="text-danger hover:underline">Remove</button>
        )}
      </div>

      {validation && (
        <p role="alert" className="text-xs text-danger">{validation}</p>
      )}
    </div>
  );
}
