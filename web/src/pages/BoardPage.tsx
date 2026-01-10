import { Link, useParams, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import type { CardSummary } from '../api/types';
import { AddColumnForm } from '../board/AddColumnForm';
import { BoardColumn } from '../board/BoardColumn';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

export function BoardPage() {
  const { boardId = '' } = useParams();
  const [, setParams] = useSearchParams();
  const board = useQuery({
    queryKey: queryKeys.board(boardId),
    queryFn: () => boardsApi.get(boardId),
  });
  const cards = useQuery({
    queryKey: queryKeys.cards(boardId),
    queryFn: () => boardsApi.cards(boardId),
  });

  if (board.isPending) {
    return <div className="page-loading">Loading board…</div>;
  }
  if (board.isError) {
    return <ErrorMessage error={board.error} />;
  }

  const byColumn = new Map<string, CardSummary[]>();
  for (const card of cards.data ?? []) {
    byColumn.set(card.columnId, [...(byColumn.get(card.columnId) ?? []), card]);
  }

  return (
    <section className="board-page">
      <header className="board-header">
        <div>
          <Link to={`/w/${board.data.workspaceId}`} className="muted">
            {board.data.workspaceName}
          </Link>
          <h1>{board.data.name}</h1>
          {board.data.description && <p className="muted">{board.data.description}</p>}
        </div>
      </header>
      <ErrorMessage error={cards.error} />
      <div className="columns">
        {board.data.columns.map((column) => (
          <BoardColumn
            key={column.id}
            boardId={boardId}
            column={column}
            cards={byColumn.get(column.id) ?? []}
            onOpenCard={(cardId) => setParams({ card: cardId })}
          />
        ))}
        <AddColumnForm boardId={boardId} />
      </div>
    </section>
  );
}
