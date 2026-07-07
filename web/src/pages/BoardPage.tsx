import { useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { AddColumnForm } from '../board/AddColumnForm';
import { ArchivedCardsPanel } from '../board/ArchivedCardsPanel';
import { BoardActivityPanel } from '../board/BoardActivityPanel';
import { BoardColumn } from '../board/BoardColumn';
import { BoardSettings } from '../board/BoardSettings';
import { CardModal } from '../board/CardModal';
import { FilterBar } from '../board/FilterBar';
import { LabelManager } from '../board/LabelManager';
import { useBoardData } from '../board/useBoardData';
import { Avatar } from '../components/Avatar';
import { ErrorMessage } from '../components/ErrorMessage';
import { StarButton } from '../components/StarButton';
import { collapsedColumns } from '../lib/collapsedColumns';
import { activeFilterCount } from '../lib/filters';
import { keyboardMoveTarget } from '../lib/keyboardMove';
import type { Direction } from '../lib/keyboardMove';
import { groupByColumn, isNoopMove } from '../lib/ordering';

export function BoardPage() {
  const { boardId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const openCardId = params.get('card');
  const [draggingId, setDraggingId] = useState<string | null>(null);
  const [showLabels, setShowLabels] = useState(false);
  const [panel, setPanel] = useState<'activity' | 'archive' | null>(null);
  const [showSettings, setShowSettings] = useState(false);
  const [collapsed, setCollapsed] = useState(() => collapsedColumns.get(boardId));

  const { board, cards, filters, setFilters, move } = useBoardData(boardId);

  const openCard = (cardId: string | null) =>
    setParams((current) => {
      const next = new URLSearchParams(current);
      if (cardId) {
        next.set('card', cardId);
      } else {
        next.delete('card');
      }
      return next;
    });

  if (board.isPending) {
    return <div className="page-loading">Loading board…</div>;
  }
  if (board.isError) {
    return <ErrorMessage error={board.error} />;
  }

  const byColumn = groupByColumn(cards.data ?? []);

  function dropCard(cardId: string, columnId: string, afterCardId: string | null) {
    setDraggingId(null);
    const card = cards.data?.find((c) => c.id === cardId);
    if (!card || isNoopMove(byColumn.get(columnId) ?? [], card, columnId, afterCardId)) {
      return;
    }
    move.mutate({ cardId, columnId, afterCardId });
  }

  function keyboardMove(cardId: string, direction: Direction) {
    const card = cards.data?.find((c) => c.id === cardId);
    if (!card || !board.data) {
      return;
    }
    const target = keyboardMoveTarget(
      card,
      byColumn.get(card.columnId) ?? [],
      board.data.columns,
      direction,
    );
    if (target) {
      move.mutate({ cardId, ...target });
      // Keep focus on the card so it can be moved again straight away.
      requestAnimationFrame(() =>
        document.querySelector<HTMLElement>(`[data-tile-id="${cardId}"]`)?.focus(),
      );
    }
  }

  return (
    <section className="board-page">
      <header className="board-header">
        <div>
          <Link to={`/w/${board.data.workspaceId}`} className="muted">
            {board.data.workspaceName}
          </Link>
          <h1>
            {board.data.name}{' '}
            <StarButton
              boardId={boardId}
              workspaceId={board.data.workspaceId}
              starred={board.data.starred}
            />
            {board.data.archivedAt && <span className="archived-badge">Archived</span>}
          </h1>
          {board.data.description && <p className="muted">{board.data.description}</p>}
        </div>
        <div className="row board-actions">
          <span className="avatar-stack" aria-label="Board members">
            {board.data.members.slice(0, 6).map((m) => (
              <Avatar key={m.userId} id={m.userId} name={m.displayName} size={28} />
            ))}
            {board.data.members.length > 6 && (
              <span className="more">+{board.data.members.length - 6}</span>
            )}
          </span>
          <button type="button" onClick={() => setShowLabels(true)}>
            Labels
          </button>
          <button
            type="button"
            aria-pressed={panel === 'activity'}
            onClick={() => setPanel((open) => (open === 'activity' ? null : 'activity'))}
          >
            Activity
          </button>
          <button
            type="button"
            aria-pressed={panel === 'archive'}
            onClick={() => setPanel((open) => (open === 'archive' ? null : 'archive'))}
          >
            Archive
          </button>
          <button type="button" onClick={() => setShowSettings(true)}>
            Settings
          </button>
        </div>
      </header>
      <FilterBar key={boardId} board={board.data} filters={filters} onChange={setFilters} />
      {activeFilterCount(filters) > 0 && cards.data && (
        <p className="muted small filter-summary">
          Showing {cards.data.length} matching {cards.data.length === 1 ? 'card' : 'cards'}
        </p>
      )}
      <ErrorMessage error={cards.error ?? move.error} />
      <div className="board-body">
        <div className="columns">
          {board.data.columns.map((column) => (
            <BoardColumn
              key={column.id}
              boardId={boardId}
              column={column}
              columnCount={board.data.columns.length}
              cards={byColumn.get(column.id) ?? []}
              labels={board.data.labels}
              members={board.data.members}
              draggingId={draggingId}
              onOpenCard={openCard}
              onDragStart={setDraggingId}
              onDragEnd={() => setDraggingId(null)}
              onDropCard={dropCard}
              onKeyboardMove={keyboardMove}
              collapsed={collapsed.includes(column.id)}
              onToggleCollapsed={() => setCollapsed(collapsedColumns.toggle(boardId, column.id))}
            />
          ))}
          <AddColumnForm boardId={boardId} />
        </div>
        {panel === 'activity' && (
          <BoardActivityPanel
            boardId={boardId}
            onClose={() => setPanel(null)}
            onOpenCard={openCard}
          />
        )}
        {panel === 'archive' && (
          <ArchivedCardsPanel
            board={board.data}
            onClose={() => setPanel(null)}
            onOpenCard={openCard}
          />
        )}
      </div>
      {showSettings && <BoardSettings board={board.data} onClose={() => setShowSettings(false)} />}
      {showLabels && <LabelManager board={board.data} onClose={() => setShowLabels(false)} />}
      {openCardId && (
        <CardModal
          key={openCardId}
          board={board.data}
          cardId={openCardId}
          onClose={() => openCard(null)}
        />
      )}
    </section>
  );
}
