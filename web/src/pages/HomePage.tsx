import { useState } from 'react';
import type { FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { workspacesApi } from '../api/workspaces';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

/** Sends people to their first workspace, or helps them create one. */
export function HomePage() {
  const workspaces = useQuery({ queryKey: queryKeys.workspaces, queryFn: workspacesApi.list });

  if (workspaces.isPending) {
    return <div className="page-loading">Loading…</div>;
  }
  if (workspaces.isError) {
    return <ErrorMessage error={workspaces.error} />;
  }
  const first = workspaces.data[0];
  if (first) {
    return <Navigate to={`/w/${first.id}`} replace />;
  }
  return (
    <section className="empty-state">
      <h1>Welcome to Taskboard</h1>
      <p className="muted">
        Start with a workspace for your team. You can invite people once it exists.
      </p>
      <CreateWorkspaceForm />
    </section>
  );
}

export function CreateWorkspaceForm() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const create = useMutation({
    mutationFn: () => workspacesApi.create(name),
    onSuccess: (workspace) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces });
      navigate(`/w/${workspace.id}`);
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <form className="inline-form" onSubmit={submit}>
      <input
        aria-label="Workspace name"
        placeholder="e.g. Northwind design team"
        value={name}
        onChange={(e) => setName(e.target.value)}
        maxLength={80}
        required
      />
      <button type="submit" className="primary" disabled={create.isPending}>
        Create workspace
      </button>
      <ErrorMessage error={create.error} />
    </form>
  );
}
