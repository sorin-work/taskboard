import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { authApi } from '../api/auth';
import { useAuth } from '../auth/AuthContext';
import { ErrorMessage } from '../components/ErrorMessage';
import { FormField } from '../components/FormField';

export function SettingsPage() {
  return (
    <section className="settings-page">
      <h1>Settings</h1>
      <ProfileForm />
      <PasswordForm />
    </section>
  );
}

function ProfileForm() {
  const { user, updateUser } = useAuth();
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const save = useMutation({
    mutationFn: () => authApi.updateProfile(displayName),
    onSuccess: updateUser,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  return (
    <form className="settings-card" onSubmit={submit}>
      <h2>Profile</h2>
      <FormField label="Email" name="email" value={user?.email ?? ''} disabled readOnly />
      <FormField
        label="Display name"
        name="displayName"
        value={displayName}
        maxLength={80}
        onChange={(e) => setDisplayName(e.target.value)}
        required
      />
      <ErrorMessage error={save.error} />
      {save.isSuccess && <p className="success">Saved.</p>}
      <button className="primary" disabled={save.isPending}>
        Save profile
      </button>
    </form>
  );
}

function PasswordForm() {
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [mismatch, setMismatch] = useState(false);
  const change = useMutation({
    mutationFn: () => authApi.changePassword(current, next),
    onSuccess: () => {
      setCurrent('');
      setNext('');
      setConfirm('');
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (next !== confirm) {
      setMismatch(true);
      return;
    }
    setMismatch(false);
    change.mutate();
  }

  return (
    <form className="settings-card" onSubmit={submit}>
      <h2>Password</h2>
      <FormField
        label="Current password"
        name="currentPassword"
        type="password"
        autoComplete="current-password"
        value={current}
        onChange={(e) => setCurrent(e.target.value)}
        required
      />
      <FormField
        label="New password"
        name="newPassword"
        type="password"
        autoComplete="new-password"
        minLength={8}
        value={next}
        onChange={(e) => setNext(e.target.value)}
        required
      />
      <FormField
        label="Repeat new password"
        name="confirmPassword"
        type="password"
        autoComplete="new-password"
        value={confirm}
        onChange={(e) => setConfirm(e.target.value)}
        error={mismatch ? "The passwords don't match." : undefined}
        required
      />
      <ErrorMessage error={change.error} />
      {change.isSuccess && <p className="success">Password changed.</p>}
      <button className="primary" disabled={change.isPending}>
        Change password
      </button>
    </form>
  );
}
