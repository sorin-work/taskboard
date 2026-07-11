import { lazy, Suspense, useState } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, Route, Routes } from 'react-router';
import { AuthProvider } from './auth/AuthContext';
import { RedirectIfSignedIn, RequireAuth } from './auth/guards';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { SignupPage } from './pages/SignupPage';
import { WorkspacePage } from './pages/WorkspacePage';
import { createQueryClient } from './queryClient';
import { Layout } from './components/Layout';

// Bigger pages load on demand so sign in and the workspace list stay small.
const BoardPage = lazy(() => import('./pages/BoardPage').then((m) => ({ default: m.BoardPage })));
const InvitePage = lazy(() =>
  import('./pages/InvitePage').then((m) => ({ default: m.InvitePage })),
);
const MembersPage = lazy(() =>
  import('./pages/MembersPage').then((m) => ({ default: m.MembersPage })),
);
const MyTasksPage = lazy(() =>
  import('./pages/MyTasksPage').then((m) => ({ default: m.MyTasksPage })),
);
const SearchPage = lazy(() =>
  import('./pages/SearchPage').then((m) => ({ default: m.SearchPage })),
);
const SettingsPage = lazy(() =>
  import('./pages/SettingsPage').then((m) => ({ default: m.SettingsPage })),
);

export function App() {
  const [queryClient] = useState(createQueryClient);
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <Suspense fallback={<div className="page-loading">Loading…</div>}>
            <Routes>
              <Route element={<RedirectIfSignedIn />}>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/signup" element={<SignupPage />} />
              </Route>
              <Route path="/invite/:token" element={<InvitePage />} />
              <Route element={<RequireAuth />}>
                <Route element={<Layout />}>
                  <Route path="/" element={<HomePage />} />
                  <Route path="/w/:workspaceId" element={<WorkspacePage />} />
                  <Route path="/w/:workspaceId/members" element={<MembersPage />} />
                  <Route path="/w/:workspaceId/search" element={<SearchPage />} />
                  <Route path="/b/:boardId" element={<BoardPage />} />
                  <Route path="/my-tasks" element={<MyTasksPage />} />
                  <Route path="/settings" element={<SettingsPage />} />
                  <Route path="*" element={<NotFoundPage />} />
                </Route>
              </Route>
            </Routes>
          </Suspense>
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
