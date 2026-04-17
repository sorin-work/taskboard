import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Board, Card } from '../api/types';
import { mockApi, renderWithProviders } from '../test/utils';
import { CardModal } from './CardModal';

const board: Board = {
  id: 'b1',
  workspaceId: 'w1',
  workspaceName: 'Studio',
  name: 'Website',
  description: '',
  role: 'owner',
  createdAt: '',
  archivedAt: null,
  columns: [
    { id: 'todo', name: 'To do', position: 0, wipLimit: null },
    { id: 'done', name: 'Done', position: 1, wipLimit: null },
  ],
  labels: [],
  members: [],
};

const card: Card = {
  id: 'c1',
  boardId: 'b1',
  columnId: 'todo',
  columnName: 'To do',
  title: 'Write release notes',
  description: '',
  position: 1024,
  dueDate: null,
  completedAt: null,
  archivedAt: null,
  createdBy: { id: 'u1', displayName: 'Dana' },
  assignees: [],
  labels: [],
  checklists: [],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
};

const summary = (id: string, columnId: string, position: number) => ({
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
});

describe('CardModal', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('moves the card to the bottom of the chosen column', async () => {
    const fetchMock = mockApi({
      'GET /api/cards/c1': () => ({ body: card }),
      'GET /api/cards/c1/comments': () => ({ body: [] }),
      'GET /api/cards/c1/activity': () => ({ body: [] }),
      'GET /api/boards/b1/cards': () => ({
        body: [
          summary('c1', 'todo', 1024),
          summary('d1', 'done', 1024),
          summary('d2', 'done', 2048),
        ],
      }),
      'POST /api/cards/c1/move': () => ({
        body: { ...card, columnId: 'done', columnName: 'Done' },
      }),
    });
    renderWithProviders(<CardModal board={board} cardId="c1" onClose={() => {}} />);

    await userEvent.selectOptions(await screen.findByLabelText('Column'), 'done');

    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([url]) => url === '/api/cards/c1/move')).toBe(true),
    );
    const moveCall = fetchMock.mock.calls.find(([url]) => url === '/api/cards/c1/move')!;
    expect(JSON.parse(moveCall[1]!.body as string)).toEqual({
      columnId: 'done',
      afterCardId: 'd2',
    });
  });
});
