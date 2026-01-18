import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { invitationsApi } from '../api/workspaces';
import { useAuth } from '../auth/AuthContext';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

export function InvitePage() {
  const { token = '' } = useParams();
  const { user, loading, signOut } = useAuth();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const preview = useQuery({
    queryKey: ['invitations', token],
    queryFn: () => invitationsApi.preview(token),
  });
  const accept = useMutation({
    mutationFn: () => invitationsApi.accept(token),
    onSuccess: (workspace) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces });
      navigate(`/w/${workspace.id}`, { replace: true });
    },
  });

  const next = encodeURIComponent(`/invite/${token}`);

  if (preview.isPending || loading) {
    return <div className="page-loading">Loading invitation…</div>;
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        {preview.isError ? (
          <>
            <h1>Invitation not found</h1>
            <p className="muted">The link may be mistyped, or the invitation was replaced.</p>
            <Link to="/">Go to Taskboard</Link>
          </>
        ) : (
          <>
            <h1>Join {preview.data.workspaceName}</h1>
            <p>
              {preview.data.invitedBy} invited <strong>{preview.data.email}</strong> to join as{' '}
              {preview.data.role === 'admin' ? 'an admin' : 'a member'}.
            </p>
            {preview.data.status === 'pending' ? (
              user ? (
                user.email === preview.data.email ? (
                  <button
                    className="primary"
                    onClick={() => accept.mutate()}
                    disabled={accept.isPending}
                  >
                    Accept invitation
                  </button>
                ) : (
                  <p className="error">
                    You're signed in as {user.email}.{' '}
                    <button type="button" className="link" onClick={signOut}>
                      Sign out
                    </button>{' '}
                    and use {preview.data.email} to accept.
                  </p>
                )
              ) : (
                <div className="row">
                  <Link className="button primary" to={`/signup?next=${next}`}>
                    Create an account
                  </Link>
                  <Link className="button" to={`/login?next=${next}`}>
                    Sign in
                  </Link>
                </div>
              )
            ) : (
              <p className="error">
                {preview.data.status === 'expired' &&
                  'This invitation has expired. Ask for a new one.'}
                {preview.data.status === 'revoked' && 'This invitation was cancelled.'}
                {preview.data.status === 'accepted' && 'This invitation has already been used.'}
              </p>
            )}
            <ErrorMessage error={accept.error} />
          </>
        )}
      </div>
    </div>
  );
}
