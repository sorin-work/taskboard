import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import type { CardFilters } from '../api/boards';
import { cardsApi } from '../api/cards';
import type { CardSummary } from '../api/types';
import { readFilters, writeFilters } from '../lib/filters';
import { moveCard } from '../lib/ordering';
import { queryKeys } from '../lib/queryKeys';

export interface MoveArgs {
  cardId: string;
  columnId: string;
  afterCardId: string | null;
}

/**
 * Everything the board page needs from the server: the board, its cards for
 * the filters in the url, and a move mutation that updates the cached cards
 * straight away and rolls back if the api refuses.
 */
export function useBoardData(boardId: string) {
  const queryClient = useQueryClient();
  const [params, setParams] = useSearchParams();

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

  return { board, cards, filters, setFilters, move };
}
