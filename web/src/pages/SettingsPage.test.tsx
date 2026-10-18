import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { tokenStore } from '../api/client';
import { mockApi, renderWithProviders } from '../test/utils';
import { SettingsPage } from './SettingsPage';

const me = { id: 'u1', email: 'demo@example.com', displayName: 'Jordan Lee' };

describe('SettingsPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('refuses mismatched new passwords without calling the api', async () => {
    tokenStore.set('t');
    const fetchMock = mockApi({ 'GET /api/auth/me': () => ({ body: me }) });
    renderWithProviders(<SettingsPage />);

    await screen.findByDisplayValue('demo@example.com');
    await userEvent.type(screen.getByLabelText('Current password'), 'demo-password');
    await userEvent.type(screen.getByLabelText('New password'), 'new-password-1');
    await userEvent.type(screen.getByLabelText('Repeat new password'), 'new-password-2');
    await userEvent.click(screen.getByRole('button', { name: 'Change password' }));

    expect(screen.getByText("The passwords don't match.")).toBeInTheDocument();
    expect(fetchMock.mock.calls.some(([url]) => url === '/api/auth/me/password')).toBe(false);
  });

  it('shows the api error for a wrong current password', async () => {
    tokenStore.set('t');
    mockApi({
      'GET /api/auth/me': () => ({ body: me }),
      'POST /api/auth/me/password': () => ({
        status: 400,
        body: { title: 'Bad request', detail: 'Your current password is not correct.' },
      }),
    });
    renderWithProviders(<SettingsPage />);

    await screen.findByDisplayValue('demo@example.com');
    await userEvent.type(screen.getByLabelText('Current password'), 'wrong');
    await userEvent.type(screen.getByLabelText('New password'), 'new-password-1');
    await userEvent.type(screen.getByLabelText('Repeat new password'), 'new-password-1');
    await userEvent.click(screen.getByRole('button', { name: 'Change password' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Your current password is not correct.',
    );
  });
});
