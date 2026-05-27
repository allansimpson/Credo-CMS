import { useEffect } from "react";
import { useEditor, EditorContent, type Editor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Link from "@tiptap/extension-link";
import Placeholder from "@tiptap/extension-placeholder";

export interface TipTapEditorProps {
  /** ProseMirror JSON serialized as a string. `null` is treated as empty. */
  valueJson: string | null;
  onChangeJson: (json: string | null) => void;
  placeholder?: string;
  ariaLabel?: string;
  minHeight?: number;
}

/**
 * Minimal TipTap editor used by Site Settings (members welcome text).
 * Pages/News will add a richer toolbar; the editor body shape is the
 * same so the storage format won't change.
 */
export function TipTapEditor({
  valueJson,
  onChangeJson,
  placeholder,
  ariaLabel,
  minHeight = 160,
}: TipTapEditorProps) {
  const initial = parseJsonOrNull(valueJson);

  const editor = useEditor({
    extensions: [
      StarterKit.configure({ heading: { levels: [2, 3] } }),
      Link.configure({ openOnClick: false, autolink: false }),
      Placeholder.configure({ placeholder: placeholder ?? "" }),
    ],
    content: initial,
    onUpdate({ editor }) {
      const json = editor.getJSON();
      const isEmpty = editor.isEmpty;
      onChangeJson(isEmpty ? null : JSON.stringify(json));
    },
    editorProps: {
      attributes: {
        class:
          "prose prose-sm max-w-none focus:outline-none px-3 py-2 [&_p]:my-2",
        "aria-label": ariaLabel ?? "Rich text editor",
      },
    },
  });

  // Keep editor in sync if the parent swaps the value externally (e.g. on
  // a fresh `getAdmin()` after save).
  useEffect(() => {
    if (!editor) return;
    const next = parseJsonOrNull(valueJson);
    const current = editor.getJSON();
    if (JSON.stringify(next) === JSON.stringify(current)) return;
    editor.commands.setContent(next ?? "");
  }, [valueJson, editor]);

  if (!editor) return null;

  return (
    <div
      className="rounded-md border bg-background"
      style={{ minHeight }}
    >
      <Toolbar editor={editor} />
      <EditorContent editor={editor} />
    </div>
  );
}

function Toolbar({ editor }: { editor: Editor }) {
  return (
    <div className="flex flex-wrap items-center gap-1 border-b bg-panel-alt/30 px-2 py-1 text-sm">
      <Btn label="B" pressed={editor.isActive("bold")}
        onClick={() => editor.chain().focus().toggleBold().run()} />
      <Btn label="I" italic pressed={editor.isActive("italic")}
        onClick={() => editor.chain().focus().toggleItalic().run()} />
      <Sep />
      <Btn label="H2" pressed={editor.isActive("heading", { level: 2 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} />
      <Btn label="H3" pressed={editor.isActive("heading", { level: 3 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} />
      <Sep />
      <Btn label="• List" pressed={editor.isActive("bulletList")}
        onClick={() => editor.chain().focus().toggleBulletList().run()} />
      <Btn label="1. List" pressed={editor.isActive("orderedList")}
        onClick={() => editor.chain().focus().toggleOrderedList().run()} />
      <Sep />
      <Btn
        label="Link"
        pressed={editor.isActive("link")}
        onClick={() => {
          const previous = editor.getAttributes("link").href as string | undefined;
          const url = window.prompt("URL", previous ?? "https://");
          if (url === null) return;
          if (url === "") {
            editor.chain().focus().unsetLink().run();
          } else {
            editor.chain().focus().extendMarkRange("link").setLink({ href: url }).run();
          }
        }}
      />
      <Sep />
      <Btn label="Clear"
        onClick={() => editor.chain().focus().clearContent().run()} />
    </div>
  );
}

function Btn({
  label, pressed, onClick, italic,
}: { label: string; pressed?: boolean; onClick: () => void; italic?: boolean }) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={pressed ?? false}
      className={
        "h-7 rounded px-2 text-xs hover:bg-panel-alt " +
        (pressed ? "bg-panel-alt font-semibold" : "") +
        (italic ? " italic" : "")
      }
    >
      {label}
    </button>
  );
}

function Sep() {
  return <span aria-hidden className="mx-1 h-4 w-px bg-border" />;
}

function parseJsonOrNull(value: string | null): object | null {
  if (!value) return null;
  try {
    return JSON.parse(value) as object;
  } catch {
    return null;
  }
}
