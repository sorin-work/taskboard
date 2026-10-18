import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { workspacesApi } from '../api/workspaces';
import { ErrorMessage } from '../components/ErrorMessage';
import { Highlight } from '../components/Highlight';
import { usePageTitle } from '../lib/usePageTitle';
import { queryKeys } from '../lib/queryKeys';

export function SearchPage() {
  const { workspaceId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const q = params.get('q') ?? '';
  const [text, setText] = useState(q);

  const workspace = useQuery({
    queryKey: queryKeys.workspace(workspaceId),
    queryFn: () => workspacesApi.get(workspaceId),
  });
  const results = useQuery({
    queryKey: queryKeys.search(workspaceId, q),
    queryFn: () => workspacesApi.search(workspaceId, q),
    enabled: q.trim().length >= 2,
  });

  usePageTitle(q ? `Search: ${q}` : 'Search');

  function submit(event: FormEvent) {
    event.preventDefault();
    setParams(text.trim() ? { q: text.trim() } : {});
  }

  return (
    <section className="search-page">
      <header className="page-header">
        <div>
          <Link to={`/w/${workspaceId}`} className="muted">
            ← {workspace.data?.name ?? 'Workspace'}
          </Link>
          <h1>Search</h1>
        </div>
      </header>
      <form className="invite-form" onSubmit={submit} role="search">
        <input
          type="search"
          aria-label="Search all boards"
          placeholder="Search all boards in this workspace…"
          value={text}
          onChange={(e) => setText(e.target.value)}
          autoFocus
        />
        <button className="primary">Search</button>
      </form>
      {q && q.trim().length < 2 && <p className="muted">Type at least two characters.</p>}
      <ErrorMessage error={results.error} />
      {results.data?.length === 0 && <p className="muted">No cards match “{q}”.</p>}
      {results.data && results.data.length > 0 && (
        <ul className="task-list">
          {results.data.map((r) => (
            <li key={r.cardId} className={r.completed ? 'task completed' : 'task'}>
              <div className="search-hit">
                <Link to={`/b/${r.boardId}?card=${r.cardId}`} className="task-title">
                  <Highlight text={r.title} term={q} />
                </Link>
                {r.snippet && (
                  <p className="muted small">
                    <Highlight text={r.snippet} term={q} />
                  </p>
                )}
              </div>
              <span className="muted small task-where">
                {r.boardName} › {r.columnName}
              </span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
