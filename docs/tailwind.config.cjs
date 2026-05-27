const sharedTheme = require("../tailwind.system-theme.cjs");

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{astro,html,js,jsx,md,mdx,svelte,ts,tsx,vue}"],
  theme: {
    extend: {
      colors: sharedTheme.colors,
      borderRadius: sharedTheme.borderRadius,
      fontFamily: sharedTheme.fontFamily,
    },
  },
  plugins: [],
};
