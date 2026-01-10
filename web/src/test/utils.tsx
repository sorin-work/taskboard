import type { ReactElement } from 'react';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router';
import { vi } from 'vitest';
import { AuthProvider } from '../auth/AuthContext';

export function renderWithProviders(
  ui: ReactElement,
  { route = '/', path = '*', extraRoutes = [] as { path: string; element: ReactElement }[] } = {},
) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>
        <AuthProvider>
          <Routes>
            <Route path={path} element={ui} />
            {extraRoutes.map((r) => (
              <Route key={r.path} path={r.path} element={r.element} />
            ))}
          </Routes>
        </AuthProvider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

type Handler = (init: RequestInit & { url: string }) => { status?: number; body?: unknown };

/** Stubs fetch with a tiny router: `{ 'POST /api/auth/login': () => ({ body }) }`. */
export function mockApi(routes: Record<string, Handler>) {
  const fetchMock = vi.fn(async (url: string, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${url.split('?')[0]}`;
    const handler = routes[key];
    if (!handler) {
      return new Response(JSON.stringify({ title: `No mock for ${key}` }), { status: 500 });
    }
    const { status = 200, body } = handler({ ...init, url });
    return new Response(body === undefined ? null : JSON.stringify(body), {
      status,
      headers: { 'Content-Type': 'application/json' },
    });
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}
