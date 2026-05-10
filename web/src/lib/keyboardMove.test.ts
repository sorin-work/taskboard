import { describe, expect, it } from 'vitest';
import type { CardSummary, Column } from '../api/types';
import { directionForKey, keyboardMoveTarget } from './keyboardMove';

const columns: Column[] = [
  { id: 'todo', name: 'To do', position: 0, wipLimit: null },
  { id: 'done', name: 'Done', position: 2, wipLimit: null },
  { id: 'doing', name: 'Doing', position: 1, wipLimit: null },
];

function card(id: string, columnId = 'todo'): CardSummary {
  return {
    id,
    columnId,
    title: id,
    position: 0,
    dueDate: null,
    completed: false,
    assigneeIds: [],
    labelIds: [],
    checklistDone: 0,
    checklistTotal: 0,
    commentCount: 0,
  };
}

const [a, b, c] = [card('a'), card('b'), card('c')];
const column = [a, b, c];

describe('keyboardMoveTarget', () => {
  it('moves up past the card above', () => {
    expect(keyboardMoveTarget(c, column, columns, 'up')).toEqual({
      columnId: 'todo',
      afterCardId: 'a',
    });
    expect(keyboardMoveTarget(b, column, columns, 'up')).toEqual({
      columnId: 'todo',
      afterCardId: null,
    });
  });

  it('moves down past the card below', () => {
    expect(keyboardMoveTarget(a, column, columns, 'down')).toEqual({
      columnId: 'todo',
      afterCardId: 'b',
    });
  });

  it('stops at the edges', () => {
    expect(keyboardMoveTarget(a, column, columns, 'up')).toBeNull();
    expect(keyboardMoveTarget(c, column, columns, 'down')).toBeNull();
    expect(keyboardMoveTarget(a, column, columns, 'left')).toBeNull();
    expect(keyboardMoveTarget(card('x', 'done'), [], columns, 'right')).toBeNull();
  });

  it('moves sideways by column position, to the top', () => {
    expect(keyboardMoveTarget(a, column, columns, 'right')).toEqual({
      columnId: 'doing',
      afterCardId: null,
    });
    expect(keyboardMoveTarget(card('x', 'done'), [], columns, 'left')).toEqual({
      columnId: 'doing',
      afterCardId: null,
    });
  });
});

describe('directionForKey', () => {
  it('maps arrow keys', () => {
    expect(directionForKey('ArrowLeft')).toBe('left');
    expect(directionForKey('Enter')).toBeNull();
  });
});
