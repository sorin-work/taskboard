import { useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { cardsApi } from '../api/cards';
import type { CardSummary } from '../api/types';
import { AddColumnForm } from '../board/AddColumnForm';
import { BoardColumn } from '../board/BoardColumn';
import { CardModal } from '../board/CardModal';
import { LabelManager } from '../board/LabelManager';
import { ErrorMessage } from '../components/ErrorMessage';
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

  const board = useQuery({
    queryKey: queryKeys.board(boardId),
    queryFn: () => boardsApi.get(boardId),
  });
  const cardsKey = queryKeys.cards(boardId);
  const cards = useQuery({ queryKey: cardsKey, queryFn: () => boardsApi.cards(boardId) });

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
          <h1>{board.data.name}</h1>
          {board.data.description && <p className="muted">{board.data.description}</p>}
        </div>
        <div className="row">
          <button type="button" onClick={() => setShowLabels(true)}>
            Labels
          </button>
        </div>
      </header>
      <ErrorMessage error={cards.error ?? move.error} />
      <div className="columns">
        {board.data.columns.map((column) => (
          <BoardColumn
            key={column.id}
            boardId={boardId}
            column={column}
            columnCount={board.data.columns.length}
            cards={byColumn.get(column.id) ?? []}
            draggingId={draggingId}
            onOpenCard={(cardId) => setParams({ card: cardId })}
            onDragStart={setDraggingId}
            onDragEnd={() => setDraggingId(null)}
            onDropCard={dropCard}
          />
        ))}
        <AddColumnForm boardId={boardId} />
      </div>
      {showLabels && <LabelManager board={board.data} onClose={() => setShowLabels(false)} />}
      {openCardId && (
        <CardModal
          key={openCardId}
          board={board.data}
          cardId={openCardId}
          onClose={() => setParams({})}
        />
      )}
    </section>
  );
}
