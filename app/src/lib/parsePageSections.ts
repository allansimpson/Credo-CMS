export interface ProseMirrorNode {
  type: string;
  content?: ProseMirrorNode[];
  attrs?: Record<string, unknown>;
  text?: string;
  marks?: { type: string }[];
}

export interface PageSection {
  heading: string | null;
  nodes: ProseMirrorNode[];
}

export function parsePageSections(bodyJson: string): PageSection[] {
  let doc: ProseMirrorNode;
  try {
    doc = JSON.parse(bodyJson);
  } catch {
    return [];
  }
  if (!doc.content) return [];

  const sections: PageSection[] = [];
  let current: PageSection = { heading: null, nodes: [] };

  for (const node of doc.content) {
    if (node.type === "heading" && (node.attrs?.level === 2)) {
      if (current.heading !== null || current.nodes.length > 0) {
        sections.push(current);
      }
      current = { heading: extractText(node), nodes: [] };
    } else {
      current.nodes.push(node);
    }
  }
  if (current.heading !== null || current.nodes.length > 0) {
    sections.push(current);
  }

  return sections;
}

export function extractText(node: ProseMirrorNode): string {
  if (node.text) return node.text;
  if (!node.content) return "";
  return node.content.map(extractText).join("");
}

export function extractParagraphTexts(nodes: ProseMirrorNode[]): string[] {
  return nodes
    .filter((n) => n.type === "paragraph")
    .map(extractText)
    .filter(Boolean);
}

export function findSection(sections: PageSection[], pattern: RegExp): PageSection | undefined {
  return sections.find((s) => s.heading && pattern.test(s.heading));
}

export function introSection(sections: PageSection[]): PageSection | undefined {
  return sections.find((s) => s.heading === null);
}

export interface H3Item {
  title: string;
  bodyNodes: ProseMirrorNode[];
}

export function extractH3Items(nodes: ProseMirrorNode[]): H3Item[] {
  const items: H3Item[] = [];
  let current: H3Item | null = null;
  for (const node of nodes) {
    if (node.type === "heading" && (node.attrs?.level === 3)) {
      if (current) items.push(current);
      current = { title: extractText(node), bodyNodes: [] };
    } else if (current) {
      current.bodyNodes.push(node);
    }
  }
  if (current) items.push(current);
  return items;
}

export interface BoldQA {
  question: string;
  answer: string;
}

export function extractBoldQA(nodes: ProseMirrorNode[]): BoldQA[] {
  const items: BoldQA[] = [];
  for (const node of nodes) {
    if (node.type !== "paragraph" || !node.content) continue;
    const boldParts: string[] = [];
    const plainParts: string[] = [];
    for (const inline of node.content) {
      const isBold = inline.marks?.some((m) => m.type === "bold");
      if (isBold && inline.text) boldParts.push(inline.text);
      else if (inline.text) plainParts.push(inline.text);
    }
    if (boldParts.length > 0 && plainParts.length > 0) {
      items.push({ question: boldParts.join("").trim(), answer: plainParts.join("").trim() });
    }
  }
  return items;
}
