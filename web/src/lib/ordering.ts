import type { CardSummary } from '../api/types';

/** Same spacing the api uses; only matters for optimistic updates. */
export const GAP = 1024;

export function groupByColumn(cards: CardSummary[]): Map<string, CardSummary[]> {
  const groups = new Map<string, CardSummary[]>();
  for (const card of cards) {
    const list = groups.get(card.columnId);
    if (list) {
      list.push(card);
    } else {
      groups.set(card.columnId, [card]);
    }
  }
  for (const list of groups.values()) {
    list.sort((a, b) => a.position - b.position);
  }
  return groups;
}

/**
 * Which card the dragged card should follow when dropped at `index` of a
 * column as it is currently displayed (the dragged card may be in that list).
 */
export function afterCardFor(
  columnCards: CardSummary[],
  draggedId: string,
  index: number,
): string | null {
  const before = columnCards.slice(0, index).filter((c) => c.id !== draggedId);
  return before.at(-1)?.id ?? null;
}

/** True when dropping would leave the card exactly where it is. */
export function isNoopMove(
  columnCards: CardSummary[],
  card: CardSummary,
  columnId: string,
  afterCardId: string | null,
): boolean {
  if (card.columnId !== columnId) {
    return false;
  }
  const index = columnCards.findIndex((c) => c.id === card.id);
  const current = index > 0 ? columnCards[index - 1]!.id : null;
  return current === afterCardId;
}

/** Applies a move locally, mirroring what the api will do. */
export function moveCard(
  cards: CardSummary[],
  cardId: string,
  columnId: string,
  afterCardId: string | null,
): CardSummary[] {
  const card = cards.find((c) => c.id === cardId);
  if (!card) {
    return cards;
  }
  const siblings = cards
    .filter((c) => c.columnId === columnId && c.id !== cardId)
    .sort((a, b) => a.position - b.position);
  const index = afterCardId ? siblings.findIndex((c) => c.id === afterCardId) + 1 : 0;
  const before = siblings[index - 1]?.position;
  const after = siblings[index]?.position;

  let position: number;
  if (before === undefined && after === undefined) {
    position = GAP;
  } else if (before === undefined) {
    position = after! - GAP;
  } else if (after === undefined) {
    position = before + GAP;
  } else {
    position = (before + after) / 2;
  }

  return cards.map((c) => (c.id === cardId ? { ...c, columnId, position } : c));
}

/** Insertion index for a pointer at `y`, given the bounding boxes of the visible tiles. */
export function dropIndex(y: number, tiles: { top: number; height: number }[]): number {
  const index = tiles.findIndex((tile) => y < tile.top + tile.height / 2);
  return index === -1 ? tiles.length : index;
}
