import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { mockApi, renderWithProviders } from '../test/utils';
import { WorkspacePage } from './WorkspacePage';

const board = (overrides: Record<string, unknown>) => ({
  id: 'b1',
  workspaceId: 'w1',
  name: 'Website',
  description: '',
  cardCount: 3,
  overdueCount: 0,
  createdAt: '',
  archivedAt: null,
  starred: false,
  ...overrides,
});

describe('WorkspacePage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('shows boards with their card and overdue counts', async () => {
    const fetchMock = mockApi({
      'GET /api/workspaces/w1': () => ({
        body: { id: 'w1', name: 'Studio', role: 'owner', memberCount: 3, createdAt: '' },
      }),
      'GET /api/workspaces/w1/boards': () => ({
        body: [
          board({ starred: true, overdueCount: 2 }),
          board({ id: 'b2', name: 'Ops', cardCount: 1 }),
        ],
      }),
    });
    renderWithProviders(<WorkspacePage />, { route: '/w/w1', path: '/w/:workspaceId' });

    expect(await screen.findByRole('heading', { name: 'Website' })).toBeInTheDocument();
    expect(screen.getByText('2 overdue')).toBeInTheDocument();
    expect(screen.getByText('1 card')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Unstar board' })).toBeInTheDocument();
    expect(screen.getByText('3 members', { exact: false })).toBeInTheDocument();
    const boardsCall = fetchMock.mock.calls.find(([url]) =>
      String(url).startsWith('/api/workspaces/w1/boards'),
    );
    expect(boardsCall![0]).toMatch(/today=\d{4}-\d{2}-\d{2}/);
  });
});
