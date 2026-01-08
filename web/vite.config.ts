/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

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
  },
});
