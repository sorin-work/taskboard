import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Board } from '../api/types';
import { mockApi, renderWithProviders } from '../test/utils';
import { BoardSettings } from './BoardSettings';

const board = {
  id: 'b1',
  workspaceId: 'w1',
  name: 'Sprint 7',
  description: '',
  role: 'admin',
  archivedAt: null,
} as unknown as Board;

describe('BoardSettings', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('archives the board', async () => {
    const fetchMock = mockApi({ 'PATCH /api/boards/b1': () => ({ body: board }) });
    renderWithProviders(<BoardSettings board={board} onClose={() => {}} />);

    await userEvent.click(screen.getByRole('button', { name: 'Archive board' }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({
      name: 'Sprint 7',
      description: '',
      archived: true,
    });
  });

  it('copies the board with its cards and opens the copy', async () => {
    const fetchMock = mockApi({
      'POST /api/boards/b1/copy': () => ({
        status: 201,
        body: { ...board, id: 'b2', name: 'Sprint 8' },
      }),
    });
    renderWithProviders(<BoardSettings board={board} onClose={() => {}} />, {
      extraRoutes: [{ path: '/b/:boardId', element: <p>copied board</p> }],
    });

    const name = screen.getByLabelText('Name of the copy');
    await userEvent.clear(name);
    await userEvent.type(name, 'Sprint 8');
    await userEvent.click(screen.getByLabelText('Include cards and their checklists'));
    await userEvent.click(screen.getByRole('button', { name: 'Copy board' }));

    expect(await screen.findByText('copied board')).toBeInTheDocument();
    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({
      name: 'Sprint 8',
      includeCards: true,
    });
  });

  it('hides archive and delete from members', () => {
    mockApi({});
    renderWithProviders(<BoardSettings board={{ ...board, role: 'member' }} onClose={() => {}} />);

    expect(screen.queryByRole('button', { name: 'Delete board' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Copy board' })).toBeInTheDocument();
  });
});
