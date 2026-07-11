import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { tokenStore } from '../api/client';
import { mockApi, renderWithProviders } from '../test/utils';
import { MembersPage } from './MembersPage';

const me = { id: 'u1', email: 'olga@example.com', displayName: 'Olga' };

function setup(role: 'owner' | 'member') {
  tokenStore.set('t');
  return mockApi({
    'GET /api/auth/me': () => ({ body: me }),
    'GET /api/workspaces/w1': () => ({
      body: { id: 'w1', name: 'Studio', role, memberCount: 2, createdAt: '' },
    }),
    'GET /api/workspaces/w1/members': () => ({
      body: [
        { userId: 'u1', email: 'olga@example.com', displayName: 'Olga', role, joinedAt: '' },
        {
          userId: 'u2',
          email: 'ivan@example.com',
          displayName: 'Ivan',
          role: 'admin',
          joinedAt: '',
        },
      ],
    }),
    'GET /api/workspaces/w1/workload': () => ({
      body: [
        { userId: 'u2', displayName: 'Ivan', open: 5, overdue: 2, dueThisWeek: 1 },
        { userId: 'u1', displayName: 'Olga', open: 0, overdue: 0, dueThisWeek: 0 },
      ],
    }),
    'GET /api/workspaces/w1/invitations': () => ({ body: [] }),
  });
}

describe('MembersPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('shows each member with their workload', async () => {
    setup('owner');
    renderWithProviders(<MembersPage />, {
      route: '/w/w1/members',
      path: '/w/:workspaceId/members',
    });

    expect(await screen.findByLabelText('Workload for Ivan')).toHaveTextContent(
      '5 open2 overdue · 1 this week',
    );
    expect(screen.getByLabelText('Workload for Olga')).toHaveTextContent('0 open');
    expect(screen.getByRole('button', { name: 'Leave' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Invite people' })).toBeInTheDocument();
  });

  it('hides management tools from members', async () => {
    setup('member');
    renderWithProviders(<MembersPage />, {
      route: '/w/w1/members',
      path: '/w/:workspaceId/members',
    });

    expect(await screen.findByText('Ivan')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Invite people' })).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Role for Ivan')).not.toBeInTheDocument();
  });
});
