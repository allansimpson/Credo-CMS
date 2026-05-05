import { useEffect } from "react";
import { useEditor, EditorContent, type Editor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Link from "@tiptap/extension-link";
import Image from "@tiptap/extension-image";
import Placeholder from "@tiptap/extension-placeholder";
import { imagesApi } from "@/lib/api/images";

export interface TipTapFullEditorProps {
  valueJson: string | null;
  onChangeJson: (json: string | null) => void;
  placeholder?: string;
  ariaLabel?: string;
  minHeight?: number;
}

/**
 * Full-feature TipTap editor for content authoring (Pages, News, Banner).
 * Supports headings, bold/italic, blockquote, code-block, lists, link,
 * image upload, and clear. Storage shape matches the minimal editor
 * (ProseMirror JSON), so a doc authored in either editor renders in the
 * other.
 */
export function TipTapFullEditor({
  valueJson,
  onChangeJson,
  placeholder,
  ariaLabel,
  minHeight = 320,
}: TipTapFullEditorProps) {
  const initial = parseJsonOrNull(valueJson);

  const editor = useEditor({
    extensions: [
      StarterKit.configure({ heading: { levels: [2, 3, 4] } }),
      Link.configure({ openOnClick: false, autolink: false }),
      Image.configure({ inline: false }),
      Placeholder.configure({ placeholder: placeholder ?? "" }),
    ],
    content: initial,
    onUpdate({ editor }) {
      const json = editor.getJSON();
      onChangeJson(editor.isEmpty ? null : JSON.stringify(json));
    },
    editorProps: {
      attributes: {
        class:
          "prose prose-sm max-w-none focus:outline-none px-4 py-3 [&_p]:my-2 [&_h2]:mt-6 [&_h3]:mt-4 [&_blockquote]:border-l-4 [&_blockquote]:pl-4 [&_blockquote]:italic [&_pre]:bg-muted [&_pre]:p-2 [&_pre]:rounded",
        "aria-label": ariaLabel ?? "Rich text editor",
      },
    },
  });

  useEffect(() => {
    if (!editor) return;
    const next = parseJsonOrNull(valueJson);
    const current = editor.getJSON();
    if (JSON.stringify(next) === JSON.stringify(current)) return;
    editor.commands.setContent(next ?? "");
  }, [valueJson, editor]);

  if (!editor) return null;

  return (
    <div className="rounded-md border bg-background" style={{ minHeight }}>
      <Toolbar editor={editor} />
      <EditorContent editor={editor} />
    </div>
  );
}

function Toolbar({ editor }: { editor: Editor }) {
  const insertImage = async () => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = "image/jpeg,image/png,image/webp";
    input.onchange = async () => {
      const file = input.files?.[0];
      if (!file) return;
      try {
        const result = await imagesApi.upload(file);
        editor.chain().focus().setImage({
          src: result.blobUrl,
          alt: file.name,
        }).run();
      } catch (err) {
        const messages =
          typeof err === "object" && err !== null && "getMessages" in err
            ? (err as { getMessages: () => string[] }).getMessages()
            : ["Image upload failed."];
        window.alert(messages[0]);
      }
    };
    input.click();
  };

  return (
    <div className="flex flex-wrap items-center gap-1 border-b bg-muted/30 px-2 py-1 text-sm">
      <Btn label="B" pressed={editor.isActive("bold")}
        onClick={() => editor.chain().focus().toggleBold().run()} />
      <Btn label="I" italic pressed={editor.isActive("italic")}
        onClick={() => editor.chain().focus().toggleItalic().run()} />
      <Btn label="S" strike pressed={editor.isActive("strike")}
        onClick={() => editor.chain().focus().toggleStrike().run()} />
      <Sep />
      <Btn label="H2" pressed={editor.isActive("heading", { level: 2 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()} />
      <Btn label="H3" pressed={editor.isActive("heading", { level: 3 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()} />
      <Btn label="H4" pressed={editor.isActive("heading", { level: 4 })}
        onClick={() => editor.chain().focus().toggleHeading({ level: 4 }).run()} />
      <Sep />
      <Btn label="• List" pressed={editor.isActive("bulletList")}
        onClick={() => editor.chain().focus().toggleBulletList().run()} />
      <Btn label="1. List" pressed={editor.isActive("orderedList")}
        onClick={() => editor.chain().focus().toggleOrderedList().run()} />
      <Btn label="❝ Quote" pressed={editor.isActive("blockquote")}
        onClick={() => editor.chain().focus().toggleBlockquote().run()} />
      <Btn label="< / >" pressed={editor.isActive("codeBlock")}
        onClick={() => editor.chain().focus().toggleCodeBlock().run()} />
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
      <Btn label="Image" onClick={insertImage} />
      <Sep />
      <Btn label="Clear" onClick={() => editor.chain().focus().clearContent().run()} />
    </div>
  );
}

interface BtnProps {
  label: string;
  pressed?: boolean;
  onClick: () => void;
  italic?: boolean;
  strike?: boolean;
}

function Btn({ label, pressed, onClick, italic, strike }: BtnProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={pressed ?? false}
      className={
        "h-7 rounded px-2 text-xs hover:bg-muted " +
        (pressed ? "bg-muted font-semibold " : "") +
        (italic ? "italic " : "") +
        (strike ? "line-through " : "")
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
