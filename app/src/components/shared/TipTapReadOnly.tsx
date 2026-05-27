import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Link from "@tiptap/extension-link";
import Image from "@tiptap/extension-image";

export interface TipTapReadOnlyProps {
  json: string | null;
  className?: string;
}

/**
 * Renders a ProseMirror JSON document in read-only mode. Used by the
 * public Pages route + future Pages/News/Banner public surfaces.
 */
export function TipTapReadOnly({ json, className }: TipTapReadOnlyProps) {
  const content = parseJsonOrNull(json);
  const editor = useEditor({
    extensions: [
      StarterKit.configure({ heading: { levels: [2, 3, 4] } }),
      Link.configure({ openOnClick: true, autolink: false }),
      Image.configure({ inline: false }),
    ],
    content,
    editable: false,
    editorProps: {
      attributes: {
        class:
          (className ??
            "prose prose-base max-w-none [&_p]:my-3 [&_h2]:mt-8 [&_h3]:mt-6 [&_blockquote]:border-l-4 [&_blockquote]:border-accent [&_blockquote]:pl-4 [&_blockquote]:italic"),
      },
    },
  });

  if (!editor) return null;
  return <EditorContent editor={editor} />;
}

function parseJsonOrNull(value: string | null): object | null {
  if (!value) return null;
  try {
    return JSON.parse(value) as object;
  } catch {
    return null;
  }
}
