/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Run tests west of UTC so date-only bugs (like parsing YYYY-MM-DD as UTC
// midnight) show up here rather than for users in the Americas.
process.env.TZ = 'America/Los_Angeles';

const apiUrl = process.env.API_URL ?? 'http://localhost:4404';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5184,
    proxy: {
      '/api': apiUrl,
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    testTimeout: 15_000,
  },
});
