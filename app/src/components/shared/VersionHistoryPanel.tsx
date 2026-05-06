/**
 * Version-history panel SHELL. Phase 1 ships this component scaffold so the
 * prop contract is fixed; concrete entity wiring and the diff renderers are
 * integrated in Phase 2 when the first versioned entity (Pages) lands.
 */

export type DiffStrategy = "prosemirror" | "html" | "text";

export interface VersionHistoryPanelProps {
  entityType: string;
  entityId: string;
  diffStrategy: DiffStrategy;
  onRestore?: (versionTimestamp: string) => void;
}

export function VersionHistoryPanel(props: VersionHistoryPanelProps) {
  return (
    <aside className="rounded-md border bg-card p-4 text-sm text-muted">
      <p>
        Version history shell for <code>{props.entityType}</code> /{" "}
        <code>{props.entityId}</code> (<code>{props.diffStrategy}</code> diff).
      </p>
      <p className="mt-2">
        Phase 2 will populate this panel with the actual list of historical
        versions, a diff renderer, and the restore action.
      </p>
    </aside>
  );
}
