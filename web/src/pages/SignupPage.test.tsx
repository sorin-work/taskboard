import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { mockApi, renderWithProviders } from '../test/utils';
import { SignupPage } from './SignupPage';

describe('SignupPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('keeps where to go next when switching to sign in', () => {
    mockApi({});
    renderWithProviders(<SignupPage />, { route: '/signup?next=/invite/tok', path: '/signup' });

    expect(screen.getByRole('link', { name: 'Sign in' })).toHaveAttribute(
      'href',
      '/login?next=%2Finvite%2Ftok',
    );
  });

  it('shows validation errors next to the fields', async () => {
    mockApi({
      'POST /api/auth/signup': () => ({
        status: 400,
        body: {
          title: 'One or more validation errors occurred.',
          errors: { Password: ["'Password' must be at least 8 characters."] },
        },
      }),
    });
    renderWithProviders(<SignupPage />);

    await userEvent.type(screen.getByLabelText('Your name'), 'Sam');
    await userEvent.type(screen.getByLabelText('Email'), 'sam@example.com');
    await userEvent.type(screen.getByLabelText('Password'), 'longenough');
    await userEvent.click(screen.getByRole('button', { name: 'Create account' }));

    expect(
      await screen.findByText("'Password' must be at least 8 characters."),
    ).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
