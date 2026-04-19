import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Board } from '../api/types';
import { mockApi, renderWithProviders } from '../test/utils';
import { ArchivedCardsPanel } from './ArchivedCardsPanel';

const board = {
  id: 'b1',
  columns: [{ id: 'todo', name: 'To do', position: 0, wipLimit: null }],
} as unknown as Board;

const archived = {
  id: 'c9',
  columnId: 'todo',
  title: 'Old idea',
  position: 1024,
  dueDate: null,
  completed: false,
  assigneeIds: [],
  labelIds: [],
  checklistDone: 0,
  checklistTotal: 0,
  commentCount: 0,
};

describe('ArchivedCardsPanel', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('lists archived cards and restores one', async () => {
    const fetchMock = mockApi({
      'GET /api/boards/b1/cards': () => ({ body: [archived] }),
      'POST /api/cards/c9/restore': () => ({ body: {} }),
    });
    renderWithProviders(
      <ArchivedCardsPanel board={board} onClose={() => {}} onOpenCard={() => {}} />,
    );

    expect(await screen.findByRole('button', { name: 'Old idea' })).toBeInTheDocument();
    expect(screen.getByText('To do')).toBeInTheDocument();
    expect(fetchMock.mock.calls[0]![0]).toBe('/api/boards/b1/cards?archived=true');

    await userEvent.click(screen.getByRole('button', { name: 'Restore' }));

    await waitFor(() =>
      expect(fetchMock.mock.calls.some(([url]) => url === '/api/cards/c9/restore')).toBe(true),
    );
  });

  it('says when nothing is archived', async () => {
    mockApi({ 'GET /api/boards/b1/cards': () => ({ body: [] }) });
    renderWithProviders(
      <ArchivedCardsPanel board={board} onClose={() => {}} onOpenCard={() => {}} />,
    );

    expect(await screen.findByText('Nothing archived on this board.')).toBeInTheDocument();
  });
});
