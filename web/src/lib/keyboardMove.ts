import type { CardSummary, Column } from '../api/types';

export type Direction = 'up' | 'down' | 'left' | 'right';

export interface MoveTarget {
  columnId: string;
  afterCardId: string | null;
}

/**
 * Where a card goes when moved one step with the keyboard. Up and down swap
 * it with its neighbour; left and right put it at the top of the next column.
 * Returns null when it's already at the edge.
 */
export function keyboardMoveTarget(
  card: CardSummary,
  columnCards: CardSummary[],
  columns: Column[],
  direction: Direction,
): MoveTarget | null {
  const index = columnCards.findIndex((c) => c.id === card.id);
  if (direction === 'up') {
    if (index <= 0) return null;
    return { columnId: card.columnId, afterCardId: columnCards[index - 2]?.id ?? null };
  }
  if (direction === 'down') {
    if (index === -1 || index >= columnCards.length - 1) return null;
    return { columnId: card.columnId, afterCardId: columnCards[index + 1]!.id };
  }
  const ordered = [...columns].sort((a, b) => a.position - b.position);
  const columnIndex = ordered.findIndex((c) => c.id === card.columnId);
  const next = ordered[columnIndex + (direction === 'left' ? -1 : 1)];
  return next ? { columnId: next.id, afterCardId: null } : null;
}

export function directionForKey(key: string): Direction | null {
  switch (key) {
    case 'ArrowUp':
      return 'up';
    case 'ArrowDown':
      return 'down';
    case 'ArrowLeft':
      return 'left';
    case 'ArrowRight':
      return 'right';
    default:
      return null;
  }
}
