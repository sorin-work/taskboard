import { Link, NavLink, Outlet, useNavigate, useParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { workspacesApi } from '../api/workspaces';
import { useAuth } from '../auth/AuthContext';
import { queryKeys } from '../lib/queryKeys';

export function Layout() {
  const { user, signOut } = useAuth();
  const navigate = useNavigate();
  const { workspaceId } = useParams();
  const workspaces = useQuery({ queryKey: queryKeys.workspaces, queryFn: workspacesApi.list });

  return (
    <div className="app">
      <header className="topbar">
        <Link to="/" className="brand">
          Taskboard
        </Link>
        {workspaces.data && workspaces.data.length > 0 && (
          <select
            aria-label="Workspace"
            className="workspace-switcher"
            value={workspaceId ?? ''}
            onChange={(e) => navigate(`/w/${e.target.value}`)}
          >
            {!workspaceId && <option value="">Choose a workspace…</option>}
            {workspaces.data.map((w) => (
              <option key={w.id} value={w.id}>
                {w.name}
              </option>
            ))}
          </select>
        )}
        <nav className="topbar-nav">
          <NavLink to="/my-tasks">My tasks</NavLink>
        </nav>
        <div className="topbar-user">
          <Link to="/settings">{user?.displayName}</Link>
          <button onClick={signOut}>Sign out</button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  );
}
