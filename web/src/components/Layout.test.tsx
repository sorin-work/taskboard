import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { tokenStore } from '../api/client';
import { mockApi, renderWithProviders } from '../test/utils';
import { Layout } from './Layout';

const me = { id: 'u1', email: 'demo@example.com', displayName: 'Jordan Lee' };

describe('Layout', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('shows how many of my tasks are overdue', async () => {
    tokenStore.set('t');
    mockApi({
      'GET /api/auth/me': () => ({ body: me }),
      'GET /api/workspaces': () => ({ body: [] }),
      'GET /api/me/tasks': ({ url }) => ({ body: url.includes('due=overdue') ? [{}, {}] : [] }),
    });
    renderWithProviders(<Layout />);

    expect(await screen.findByTitle('2 overdue')).toHaveTextContent('2');
    expect(await screen.findByRole('link', { name: 'Jordan Lee' })).toHaveAttribute(
      'href',
      '/settings',
    );
  });

  it('lists workspaces in the switcher', async () => {
    tokenStore.set('t');
    mockApi({
      'GET /api/auth/me': () => ({ body: me }),
      'GET /api/workspaces': () => ({
        body: [
          { id: 'w1', name: 'Home', role: 'owner', memberCount: 1, createdAt: '' },
          { id: 'w2', name: 'Studio', role: 'admin', memberCount: 4, createdAt: '' },
        ],
      }),
      'GET /api/me/tasks': () => ({ body: [] }),
    });
    renderWithProviders(<Layout />);

    const switcher = await screen.findByLabelText('Workspace');
    expect(Array.from((switcher as HTMLSelectElement).options).map((o) => o.text)).toEqual([
      'Choose a workspace…',
      'Home',
      'Studio',
    ]);
    expect(screen.queryByTitle(/overdue/)).not.toBeInTheDocument();
  });
});
