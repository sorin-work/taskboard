import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { workspacesApi } from '../api/workspaces';
import { ErrorMessage } from '../components/ErrorMessage';
import { StarButton } from '../components/StarButton';
import { todayIso } from '../lib/dates';
import { lastWorkspace } from '../lib/lastWorkspace';
import { usePageTitle } from '../lib/usePageTitle';
import { queryKeys } from '../lib/queryKeys';

export function WorkspacePage() {
  const { workspaceId = '' } = useParams();
  const [showArchived, setShowArchived] = useState(false);
  const workspace = useQuery({
    queryKey: queryKeys.workspace(workspaceId),
    queryFn: () => workspacesApi.get(workspaceId),
  });
  usePageTitle(workspace.data?.name);
  const boards = useQuery({
    queryKey: queryKeys.boards(workspaceId, showArchived),
    queryFn: () => boardsApi.list(workspaceId, showArchived, todayIso()),
  });

  useEffect(() => {
    if (workspace.data) {
      lastWorkspace.set(workspace.data.id);
    }
  }, [workspace.data]);

  if (workspace.isError) {
    return <ErrorMessage error={workspace.error} />;
  }

  return (
    <section className="workspace-page">
      <header className="page-header">
        <div>
          <h1>{workspace.data?.name ?? '…'}</h1>
          {workspace.data && (
            <p className="muted">
              {workspace.data.memberCount} {workspace.data.memberCount === 1 ? 'member' : 'members'}{' '}
              · <Link to={`/w/${workspaceId}/members`}>Manage members</Link> ·{' '}
              <Link to={`/w/${workspaceId}/search`}>Search cards</Link>
            </p>
          )}
        </div>
        <label className="checkbox">
          <input
            type="checkbox"
            checked={showArchived}
            onChange={(e) => setShowArchived(e.target.checked)}
          />
          Show archived boards
        </label>
      </header>

      <ErrorMessage error={boards.error} />
      <ul className="board-grid">
        {boards.data?.map((board) => (
          <li key={board.id} className={board.archivedAt ? 'board-tile archived' : 'board-tile'}>
            <Link to={`/b/${board.id}`}>
              <div className="board-tile-title">
                <h2>{board.name}</h2>
                <StarButton boardId={board.id} workspaceId={workspaceId} starred={board.starred} />
              </div>
              {board.description && <p>{board.description}</p>}
              <span className="muted">
                {board.cardCount} {board.cardCount === 1 ? 'card' : 'cards'}
                {board.archivedAt && ' · archived'}
              </span>
              {board.overdueCount > 0 && (
                <span className="due-badge due-overdue">{board.overdueCount} overdue</span>
              )}
            </Link>
          </li>
        ))}
        <li className="board-tile new">
          <NewBoardForm workspaceId={workspaceId} />
        </li>
      </ul>
    </section>
  );
}

function NewBoardForm({ workspaceId }: { workspaceId: string }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState('');
  const create = useMutation({
    mutationFn: () => boardsApi.create(workspaceId, name),
    onSuccess: (board) => {
      queryClient.invalidateQueries({ queryKey: ['workspaces', workspaceId, 'boards'] });
      navigate(`/b/${board.id}`);
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    create.mutate();
  }

  return (
    <form onSubmit={submit} className="new-board-form">
      <h2>New board</h2>
      <input
        aria-label="Board name"
        placeholder="Board name"
        value={name}
        onChange={(e) => setName(e.target.value)}
        maxLength={100}
        required
      />
      <button type="submit" className="primary" disabled={create.isPending}>
        Create board
      </button>
      <ErrorMessage error={create.error} />
    </form>
  );
}
