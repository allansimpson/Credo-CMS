import { useEffect } from "react";
import { useNotificationHub } from "./useNotificationHub";

/**
 * SignalR shape for prayer-request events. Backend emits the same record
 * (PrayerRequestEventMessage) on the "members" group; the SPA decodes by
 * <c>kind</c> here.
 */
export interface PrayerRequestEvent {
  kind:
    | "PrayerRequestCreated"
    | "PrayerRequestUpdated"
    | "PrayerRequestStatusChanged"
    | "PrayerRequestPrayedForCountChanged"
    | "PrayerRequestUpdateAdded";
  prayerRequestId: string;
  title: string;
  prayedForCount: number | null;
}

/**
 * Subscribes to all five prayer-request SignalR events. Pass a single
 * callback that receives every event; consumers (list page, detail page,
 * admin moderation view) discriminate on <c>kind</c>.
 *
 * If <c>prayerRequestId</c> is provided, only events whose id matches are
 * forwarded — useful on the detail page where the broader stream would be
 * noise.
 */
export function usePrayerRequestUpdates(
  onEvent: (event: PrayerRequestEvent) => void,
  prayerRequestId?: string | null,
): void {
  const { on, off } = useNotificationHub();

  useEffect(() => {
    const wrap = (kind: PrayerRequestEvent["kind"]) =>
      (...args: unknown[]) => {
        const payload = args[0] as PrayerRequestEvent | undefined;
        if (!payload) return;
        if (prayerRequestId && payload.prayerRequestId !== prayerRequestId) return;
        onEvent({ ...payload, kind });
      };

    const handlers = (
      [
        "PrayerRequestCreated",
        "PrayerRequestUpdated",
        "PrayerRequestStatusChanged",
        "PrayerRequestPrayedForCountChanged",
        "PrayerRequestUpdateAdded",
      ] as const
    ).map((kind) => [kind, wrap(kind)] as const);

    for (const [kind, handler] of handlers) on(kind, handler);
    return () => {
      for (const [kind, handler] of handlers) off(kind, handler);
    };
  }, [on, off, onEvent, prayerRequestId]);
}
