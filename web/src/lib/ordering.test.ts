import { describe, expect, it } from 'vitest';
import type { CardSummary } from '../api/types';
import { afterCardFor, dropIndex, groupByColumn, isNoopMove, moveCard } from './ordering';

function card(id: string, columnId: string, position: number): CardSummary {
  return {
    id,
    columnId,
    title: id,
    position,
    dueDate: null,
    completed: false,
    assigneeIds: [],
    labelIds: [],
    checklistDone: 0,
    checklistTotal: 0,
    commentCount: 0,
  };
}

const titles = (cards: CardSummary[] | undefined) => (cards ?? []).map((c) => c.id);

describe('groupByColumn', () => {
  it('groups and sorts by position', () => {
    const groups = groupByColumn([
      card('b', 'todo', 2048),
      card('x', 'done', 1),
      card('a', 'todo', 1024),
    ]);
    expect(titles(groups.get('todo'))).toEqual(['a', 'b']);
    expect(titles(groups.get('done'))).toEqual(['x']);
  });
});

describe('afterCardFor', () => {
  const column = [card('a', 'c', 1), card('b', 'c', 2), card('c', 'c', 3)];

  it('returns null when dropping at the top', () => {
    expect(afterCardFor(column, 'z', 0)).toBeNull();
  });

  it('returns the card above the drop point', () => {
    expect(afterCardFor(column, 'z', 2)).toBe('b');
  });

  it('ignores the dragged card itself', () => {
    expect(afterCardFor(column, 'b', 2)).toBe('a');
    expect(afterCardFor(column, 'a', 1)).toBeNull();
  });
});

describe('isNoopMove', () => {
  const column = [card('a', 'c', 1), card('b', 'c', 2)];

  it('detects dropping a card back where it was', () => {
    expect(isNoopMove(column, column[1]!, 'c', 'a')).toBe(true);
    expect(isNoopMove(column, column[0]!, 'c', null)).toBe(true);
  });

  it('treats other spots and columns as real moves', () => {
    expect(isNoopMove(column, column[0]!, 'c', 'b')).toBe(false);
    expect(isNoopMove(column, column[0]!, 'other', null)).toBe(false);
  });
});

describe('moveCard', () => {
  const cards = [card('a', 'todo', 1024), card('b', 'todo', 2048), card('x', 'done', 1024)];

  it('moves to the top of another column', () => {
    const moved = moveCard(cards, 'b', 'done', null);
    expect(titles(groupByColumn(moved).get('done'))).toEqual(['b', 'x']);
    expect(titles(groupByColumn(moved).get('todo'))).toEqual(['a']);
  });

  it('moves between two cards', () => {
    const moved = moveCard([...cards, card('c', 'todo', 3072)], 'c', 'todo', 'a');
    expect(titles(groupByColumn(moved).get('todo'))).toEqual(['a', 'c', 'b']);
  });

  it('moves to the bottom', () => {
    const moved = moveCard(cards, 'a', 'todo', 'b');
    expect(titles(groupByColumn(moved).get('todo'))).toEqual(['b', 'a']);
  });

  it('leaves the list alone for unknown cards', () => {
    expect(moveCard(cards, 'nope', 'todo', null)).toBe(cards);
  });
});

describe('dropIndex', () => {
  const tiles = [
    { top: 0, height: 40 },
    { top: 50, height: 40 },
  ];

  it.each([
    [5, 0],
    [25, 1],
    [60, 1],
    [80, 2],
    [500, 2],
  ])('y=%i -> %i', (y, expected) => {
    expect(dropIndex(y, tiles)).toBe(expected);
  });
});
