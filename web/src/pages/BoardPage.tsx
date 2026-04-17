import { useCallback, useMemo, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import type { CardFilters } from '../api/boards';
import { cardsApi } from '../api/cards';
import type { CardSummary } from '../api/types';
import { AddColumnForm } from '../board/AddColumnForm';
import { ArchivedCardsPanel } from '../board/ArchivedCardsPanel';
import { BoardActivityPanel } from '../board/BoardActivityPanel';
import { BoardColumn } from '../board/BoardColumn';
import { BoardSettings } from '../board/BoardSettings';
import { CardModal } from '../board/CardModal';
import { FilterBar } from '../board/FilterBar';
import { LabelManager } from '../board/LabelManager';
import { Avatar } from '../components/Avatar';
import { ErrorMessage } from '../components/ErrorMessage';
import { activeFilterCount, readFilters, writeFilters } from '../lib/filters';
import { groupByColumn, isNoopMove, moveCard } from '../lib/ordering';
import { queryKeys } from '../lib/queryKeys';

interface MoveArgs {
  cardId: string;
  columnId: string;
  afterCardId: string | null;
}

export function BoardPage() {
  const { boardId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const openCardId = params.get('card');
  const queryClient = useQueryClient();
  const [draggingId, setDraggingId] = useState<string | null>(null);
  const [showLabels, setShowLabels] = useState(false);
  const [panel, setPanel] = useState<'activity' | 'archive' | null>(null);
  const [showSettings, setShowSettings] = useState(false);

  const board = useQuery({
    queryKey: queryKeys.board(boardId),
    queryFn: () => boardsApi.get(boardId),
  });
  // Only the filter params matter here; opening a card shouldn't refetch.
  const filterKey = writeFilters(new URLSearchParams(), readFilters(params)).toString();
  const filters = useMemo(() => readFilters(new URLSearchParams(filterKey)), [filterKey]);
  const cardsKey = queryKeys.cards(boardId, { ...filters });
  const cards = useQuery({
    queryKey: cardsKey,
    queryFn: () => boardsApi.cards(boardId, filters),
    placeholderData: (previous) => previous,
  });

  const setFilters = useCallback(
    (next: CardFilters) => setParams((current) => writeFilters(current, next), { replace: true }),
    [setParams],
  );
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

  const move = useMutation({
    mutationFn: ({ cardId, columnId, afterCardId }: MoveArgs) =>
      cardsApi.move(cardId, columnId, afterCardId),
    onMutate: async ({ cardId, columnId, afterCardId }) => {
      await queryClient.cancelQueries({ queryKey: cardsKey });
      const previous = queryClient.getQueryData<CardSummary[]>(cardsKey);
      if (previous) {
        queryClient.setQueryData(cardsKey, moveCard(previous, cardId, columnId, afterCardId));
      }
      return { previous };
    },
    onError: (_error, _args, context) => {
      if (context?.previous) {
        queryClient.setQueryData(cardsKey, context.previous);
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'cards'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.boardActivity(boardId) });
    },
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

  return (
    <section className="board-page">
      <header className="board-header">
        <div>
          <Link to={`/w/${board.data.workspaceId}`} className="muted">
            {board.data.workspaceName}
          </Link>
          <h1>
            {board.data.name}
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
