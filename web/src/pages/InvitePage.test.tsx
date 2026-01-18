import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { tokenStore } from '../api/client';
import { mockApi, renderWithProviders } from '../test/utils';
import { InvitePage } from './InvitePage';

const preview = {
  workspaceId: 'w1',
  workspaceName: 'Northwind',
  invitedBy: 'Olga',
  email: 'sam@example.com',
  role: 'member',
  expiresAt: '2026-02-01T00:00:00Z',
  status: 'pending',
};

describe('InvitePage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('asks signed-out visitors to sign up or sign in', async () => {
    mockApi({ 'GET /api/invitations/tok': () => ({ body: preview }) });
    renderWithProviders(<InvitePage />, { route: '/invite/tok', path: '/invite/:token' });

    expect(await screen.findByRole('heading', { name: 'Join Northwind' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute(
      'href',
      '/login?next=%2Finvite%2Ftok',
    );
  });

  it('lets the invited user accept', async () => {
    tokenStore.set('t');
    const fetchMock = mockApi({
      'GET /api/auth/me': () => ({
        body: { id: 'u1', email: 'sam@example.com', displayName: 'Sam' },
      }),
      'GET /api/invitations/tok': () => ({ body: preview }),
      'POST /api/invitations/tok/accept': () => ({
        body: { id: 'w1', name: 'Northwind', role: 'member', memberCount: 3, createdAt: '' },
      }),
    });
    renderWithProviders(<InvitePage />, {
      route: '/invite/tok',
      path: '/invite/:token',
      extraRoutes: [{ path: '/w/:id', element: <p>workspace page</p> }],
    });

    await userEvent.click(await screen.findByRole('button', { name: 'Accept invitation' }));

    expect(await screen.findByText('workspace page')).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => url === '/api/invitations/tok/accept')).toBe(true);
  });

  it('explains when the invitation is for someone else', async () => {
    tokenStore.set('t');
    mockApi({
      'GET /api/auth/me': () => ({
        body: { id: 'u2', email: 'other@example.com', displayName: 'O' },
      }),
      'GET /api/invitations/tok': () => ({ body: preview }),
    });
    renderWithProviders(<InvitePage />, { route: '/invite/tok', path: '/invite/:token' });

    expect(await screen.findByText(/signed in as other@example.com/)).toBeInTheDocument();
  });

  it('says when the invitation has expired', async () => {
    mockApi({ 'GET /api/invitations/tok': () => ({ body: { ...preview, status: 'expired' } }) });
    renderWithProviders(<InvitePage />, { route: '/invite/tok', path: '/invite/:token' });

    expect(await screen.findByText(/has expired/)).toBeInTheDocument();
  });
});
