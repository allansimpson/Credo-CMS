import { defineConfig } from "astro/config";
import tailwind from "@astrojs/tailwind";

// Phase 6 Astro docs site. Static output to dist/, served from the API's
// wwwroot/docs/ in production behind the AdminShell auth gate.
export default defineConfig({
  base: "/docs",
  output: "static",
  trailingSlash: "ignore",
  build: {
    assets: "_assets",
  },
  integrations: [
    tailwind({
      // Pull in the SPA's system-theme tokens via the shared file at repo
      // root so the docs visually match the back-office UI.
      configFile: "./tailwind.config.cjs",
    }),
  ],
});
