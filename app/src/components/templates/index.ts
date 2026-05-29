import { lazy } from "react";
import type { ComponentType } from "react";
import type { PageTemplate, PublicPage } from "@/types/api";

type TemplateComponent = React.LazyExoticComponent<ComponentType<{ page: PublicPage }>>;

export const TEMPLATE_COMPONENTS: Partial<Record<PageTemplate, TemplateComponent>> = {
  About: lazy(() => import("./AboutTemplate")),
  ImNew: lazy(() => import("./ImNewTemplate")),
  Beliefs: lazy(() => import("./BeliefsTemplate")),
  Contact: lazy(() => import("./ContactTemplate")),
};
