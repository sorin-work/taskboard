import { Link } from 'react-router';

export function NotFoundPage() {
  return (
    <section className="empty-state">
      <h1>Page not found</h1>
      <p className="muted">There's nothing at this address.</p>
      <Link to="/">Back to your boards</Link>
    </section>
  );
}
