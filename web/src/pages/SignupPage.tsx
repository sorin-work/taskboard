import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useMutation } from '@tanstack/react-query';
import { authApi } from '../api/auth';
import { ApiError } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { ErrorMessage } from '../components/ErrorMessage';
import { FormField } from '../components/FormField';
import { safeNext } from '../lib/navigation';

export function SignupPage() {
  const { signIn } = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const signup = useMutation({
    mutationFn: () => authApi.signup(email, password, displayName),
    onSuccess: (response) => {
      signIn(response);
      navigate(safeNext(params.get('next')), { replace: true });
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    signup.mutate();
  }

  const fieldError = (name: string) =>
    signup.error instanceof ApiError ? signup.error.fieldError(name) : undefined;
  const hasFieldErrors =
    signup.error instanceof ApiError && Object.keys(signup.error.fieldErrors).length > 0;

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={submit}>
        <h1>Create your account</h1>
        <FormField
          label="Your name"
          name="displayName"
          autoComplete="name"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          error={fieldError('displayName')}
          required
        />
        <FormField
          label="Email"
          name="email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          error={fieldError('email')}
          required
        />
        <FormField
          label="Password"
          name="password"
          type="password"
          autoComplete="new-password"
          minLength={8}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          error={fieldError('password')}
          required
        />
        {!hasFieldErrors && <ErrorMessage error={signup.error} />}
        <button type="submit" className="primary" disabled={signup.isPending}>
          {signup.isPending ? 'Creating account…' : 'Create account'}
        </button>
        <p className="muted">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </div>
  );
}
