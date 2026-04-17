import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { cardsApi } from '../api/cards';
import type { Board } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

interface ArchivedCardsPanelProps {
  board: Board;
  onClose: () => void;
  onOpenCard: (cardId: string) => void;
}

export function ArchivedCardsPanel({ board, onClose, onOpenCard }: ArchivedCardsPanelProps) {
  const queryClient = useQueryClient();
  const archived = useQuery({
    queryKey: queryKeys.archivedCards(board.id),
    queryFn: () => boardsApi.archivedCards(board.id),
  });
  const restore = useMutation({
    mutationFn: (cardId: string) => cardsApi.restore(cardId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['boards', board.id, 'cards'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.boardActivity(board.id) });
    },
  });
  const columnName = (id: string) => board.columns.find((c) => c.id === id)?.name ?? '';

  return (
    <aside className="side-panel" aria-label="Archived cards">
      <header>
        <h2>Archived cards</h2>
        <button type="button" className="icon-button" aria-label="Close archive" onClick={onClose}>
          ×
        </button>
      </header>
      <ErrorMessage error={archived.error ?? restore.error} />
      {archived.data?.length === 0 && (
        <p className="muted small">Nothing archived on this board.</p>
      )}
      <ul className="archived-list">
        {archived.data?.map((card) => (
          <li key={card.id}>
            <button type="button" className="link" onClick={() => onOpenCard(card.id)}>
              {card.title}
            </button>
            <span className="muted small">{columnName(card.columnId)}</span>
            <button
              type="button"
              onClick={() => restore.mutate(card.id)}
              disabled={restore.isPending}
            >
              Restore
            </button>
          </li>
        ))}
      </ul>
    </aside>
  );
}
