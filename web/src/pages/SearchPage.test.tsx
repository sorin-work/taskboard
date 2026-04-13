import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { mockApi, renderWithProviders } from '../test/utils';
import { SearchPage } from './SearchPage';

const workspace = { id: 'w1', name: 'Studio', role: 'owner', memberCount: 2, createdAt: '' };

describe('SearchPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('searches and highlights matches', async () => {
    const fetchMock = mockApi({
      'GET /api/workspaces/w1': () => ({ body: workspace }),
      'GET /api/workspaces/w1/search': () => ({
        body: [
          {
            cardId: 'c1',
            title: 'Invoice page',
            snippet: null,
            boardId: 'b1',
            boardName: 'Website',
            columnName: 'To do',
            dueDate: null,
            completed: false,
          },
        ],
      }),
    });
    renderWithProviders(<SearchPage />, { route: '/w/w1/search', path: '/w/:workspaceId/search' });

    await userEvent.type(screen.getByLabelText('Search all boards'), 'invoice{Enter}');

    const link = await screen.findByRole('link', { name: 'Invoice page' });
    expect(link).toHaveAttribute('href', '/b/b1?card=c1');
    expect(link.querySelector('mark')).toHaveTextContent('Invoice');
    expect(
      fetchMock.mock.calls.some(([url]) => url === '/api/workspaces/w1/search?q=invoice'),
    ).toBe(true);
  });

  it('asks for at least two characters', async () => {
    mockApi({ 'GET /api/workspaces/w1': () => ({ body: workspace }) });
    renderWithProviders(<SearchPage />, {
      route: '/w/w1/search?q=a',
      path: '/w/:workspaceId/search',
    });

    expect(await screen.findByText('Type at least two characters.')).toBeInTheDocument();
  });
});
