import type { ServiceType } from "@/lib/api/sermons";

export interface ServiceTypeInfo {
  label: string;
  shortLabel: string;
  defaultTime: string;
  dotClass: string;
  chipTone: "muted" | "accent";
}

const INFO: Record<ServiceType, ServiceTypeInfo> = {
  AmBibleClass: {
    label: "AM Bible Class",
    shortLabel: "AM Bible Class",
    defaultTime: "9:00 AM",
    dotClass: "bg-muted",
    chipTone: "muted",
  },
  AmWorship: {
    label: "AM Worship",
    shortLabel: "AM Worship",
    defaultTime: "10:30 AM",
    dotClass: "bg-foreground",
    chipTone: "muted",
  },
  PmWorship: {
    label: "PM Worship",
    shortLabel: "PM Worship",
    defaultTime: "6:00 PM",
    dotClass: "bg-accent",
    chipTone: "accent",
  },
  WednesdayNight: {
    label: "Wednesday Night",
    shortLabel: "Wednesday Night",
    defaultTime: "7:00 PM",
    dotClass: "bg-accent",
    chipTone: "accent",
  },
  Special: {
    label: "Special",
    shortLabel: "Special",
    defaultTime: "",
    dotClass: "bg-accent",
    chipTone: "accent",
  },
};

export function getServiceTypeInfo(type: ServiceType): ServiceTypeInfo {
  return INFO[type] ?? INFO.AmWorship;
}

export function getHeroServiceType(sermons: { serviceType: ServiceType }[]): ServiceType {
  if (sermons.some((s) => s.serviceType === "AmWorship")) return "AmWorship";
  if (sermons.some((s) => s.serviceType === "PmWorship")) return "PmWorship";
  if (sermons.some((s) => s.serviceType === "AmBibleClass")) return "AmBibleClass";
  if (sermons.some((s) => s.serviceType === "WednesdayNight")) return "WednesdayNight";
  return sermons[0]?.serviceType ?? "AmWorship";
}
