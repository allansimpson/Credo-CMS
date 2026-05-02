import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { siteSettingsApi } from "@/lib/api/siteSettings";
import type { PublicSiteSettings } from "@/types/api";

interface SiteSettingsContextValue {
  settings: PublicSiteSettings | null;
  reload: () => Promise<void>;
}

const SiteSettingsContext = createContext<SiteSettingsContextValue | undefined>(undefined);

export function SiteSettingsProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<PublicSiteSettings | null>(null);

  const reload = async () => {
    try {
      const s = await siteSettingsApi.getPublic();
      setSettings(s);
    } catch {
      // Silent fall-through — public site renders with defaults if the
      // settings endpoint is unavailable.
    }
  };

  useEffect(() => {
    reload();
  }, []);

  const value = useMemo(() => ({ settings, reload }), [settings]);

  return (
    <SiteSettingsContext.Provider value={value}>
      {children}
    </SiteSettingsContext.Provider>
  );
}

export function useSiteSettings() {
  const ctx = useContext(SiteSettingsContext);
  if (!ctx) {
    throw new Error("useSiteSettings must be used inside SiteSettingsProvider");
  }
  return ctx;
}
