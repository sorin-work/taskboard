import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { tokenStore } from '../api/client';
import { mockApi, renderWithProviders } from '../test/utils';
import { LoginPage } from './LoginPage';

const user = { id: 'u1', email: 'demo@example.com', displayName: 'Demo' };

describe('LoginPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('signs in and goes to the page the user wanted', async () => {
    const fetchMock = mockApi({
      'POST /api/auth/login': () => ({ body: { token: 'tok', user } }),
    });
    renderWithProviders(<LoginPage />, {
      route: '/login?next=/boards/b1',
      path: '/login',
      extraRoutes: [{ path: '/boards/:id', element: <p>board page</p> }],
    });

    await userEvent.type(screen.getByLabelText('Email'), 'demo@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'demo-password');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByText('board page')).toBeInTheDocument();
    expect(tokenStore.get()).toBe('tok');
    const body = JSON.parse(fetchMock.mock.calls[0]![1]!.body as string);
    expect(body).toEqual({ email: 'demo@example.com', password: 'demo-password' });
  });

  it('shows the error from the api', async () => {
    mockApi({
      'POST /api/auth/login': () => ({
        status: 401,
        body: { title: 'Unauthorized', detail: 'Invalid email or password.' },
      }),
    });
    renderWithProviders(<LoginPage />);

    await userEvent.type(screen.getByLabelText('Email'), 'demo@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'nope');
    await userEvent.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Invalid email or password.');
    expect(tokenStore.get()).toBeNull();
  });
});
