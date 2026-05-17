import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      tseslint.configs.recommended,
      reactHooks.configs['recommended-latest'],
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
    },
    rules: {
      // HMR-optimization rule; conflicts with the co-located
      // context+provider+hook pattern used across the codebase
      // (AuthContext, SiteSettingsContext, etc.). Disabled until
      // those contexts are refactored to separate files — tracked
      // in ROADMAP.md.
      'react-refresh/only-export-components': 'off',
    },
  },
])
