import { useAuth } from '../auth/AuthContext';

export function HomePage() {
  const { user, signOut } = useAuth();
  return (
    <main className="placeholder">
      <h1>Hi, {user?.displayName}</h1>
      <button onClick={signOut}>Sign out</button>
    </main>
  );
}
