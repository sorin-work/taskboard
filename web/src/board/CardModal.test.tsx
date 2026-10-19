import { fireEvent, screen, waitFor } from '@testing-library/react';
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
  starred: false,
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

const cardRoutes = {
  'GET /api/cards/c1': () => ({ body: card }),
  'GET /api/cards/c1/comments': () => ({ body: [] }),
  'GET /api/cards/c1/activity': () => ({ body: [] }),
};

describe('CardModal', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('saves the title on enter', async () => {
    const fetchMock = mockApi({
      ...cardRoutes,
      'PUT /api/cards/c1': () => ({ body: { ...card, title: 'Write the release notes' } }),
    });
    renderWithProviders(<CardModal board={board} cardId="c1" onClose={() => {}} />);

    const title = await screen.findByLabelText('Title');
    await userEvent.clear(title);
    await userEvent.type(title, 'Write the release notes{Enter}');

    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(true),
    );
  });

  it('waits for a full year before saving a typed due date', async () => {
    const fetchMock = mockApi({
      ...cardRoutes,
      'PUT /api/cards/c1': () => ({ body: { ...card, dueDate: '2026-05-01' } }),
    });
    renderWithProviders(<CardModal board={board} cardId="c1" onClose={() => {}} />);

    const due = await screen.findByLabelText('Due date');
    fireEvent.change(due, { target: { value: '0002-05-01' } });
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(false);

    fireEvent.change(due, { target: { value: '2026-05-01' } });
    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(true),
    );
  });

  it('throws away a title edit on escape', async () => {
    const fetchMock = mockApi(cardRoutes);
    const onClose = vi.fn();
    renderWithProviders(<CardModal board={board} cardId="c1" onClose={onClose} />);

    const title = await screen.findByLabelText('Title');
    await userEvent.type(title, ' oops');
    await userEvent.keyboard('{Escape}');

    expect(title).toHaveValue('Write release notes');
    expect(fetchMock.mock.calls.some(([, init]) => init?.method === 'PUT')).toBe(false);
    expect(onClose).not.toHaveBeenCalled();
  });

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
