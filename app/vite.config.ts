/// <reference types="vitest" />
import path from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // Target matches the https profile in api/CredoCms.Api/Properties/launchSettings.json.
      // We target HTTPS directly so the API's UseHttpsRedirection() middleware doesn't
      // 30x us to a different origin (which the browser then blocks via CORS).
      // secure:false skips dev-cert validation. Override with VITE_API_TARGET if needed.
      "/api": {
        target: process.env.VITE_API_TARGET ?? "https://localhost:7194",
        changeOrigin: true,
        secure: false,
      },
      "/hubs": {
        target: process.env.VITE_API_TARGET ?? "https://localhost:7194",
        changeOrigin: true,
        ws: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: "dist",
    sourcemap: true,
  },
  test: {
    globals: true,
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    css: true,
  },
});
