import { useCallback } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { queryKeys } from '../lib/queryKeys';

/**
 * Refetches everything that shows a card after it changed: the card itself,
 * its activity, the board's cards and the board's activity panel.
 */
export function useRefreshCard(boardId: string, cardId: string) {
  const queryClient = useQueryClient();
  return useCallback(() => {
    queryClient.invalidateQueries({ queryKey: queryKeys.card(cardId) });
    queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'cards'] });
    queryClient.invalidateQueries({ queryKey: queryKeys.boardActivity(boardId) });
  }, [queryClient, boardId, cardId]);
}
