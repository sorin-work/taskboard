import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { CreatedInvitation, Member, Role, Workload } from '../api/types';
import { workspacesApi } from '../api/workspaces';
import { useAuth } from '../auth/AuthContext';
import { Avatar } from '../components/Avatar';
import { ErrorMessage } from '../components/ErrorMessage';
import { formatRelative, todayIso } from '../lib/dates';
import { queryKeys } from '../lib/queryKeys';

/**
 * navigator.clipboard only exists on https (and localhost). When the app is
 * opened over plain http on a LAN address, fall back to selecting the field
 * so the user can press Ctrl+C.
 */
async function copyLink(link: string): Promise<boolean> {
  try {
    if (navigator.clipboard) {
      await navigator.clipboard.writeText(link);
      return true;
    }
  } catch {
    // Permission denied: fall through to selecting the text.
  }
  const field = document.querySelector<HTMLInputElement>('input[aria-label="Invitation link"]');
  field?.select();
  return false;
}

const ROLE_LABELS: Record<Role, string> = { owner: 'Owner', admin: 'Admin', member: 'Member' };

export function MembersPage() {
  const { workspaceId = '' } = useParams();
  const workspace = useQuery({
    queryKey: queryKeys.workspace(workspaceId),
    queryFn: () => workspacesApi.get(workspaceId),
  });
  const members = useQuery({
    queryKey: queryKeys.members(workspaceId),
    queryFn: () => workspacesApi.members(workspaceId),
  });

  const workload = useQuery({
    queryKey: ['workspaces', workspaceId, 'workload'],
    queryFn: () => workspacesApi.workload(workspaceId, todayIso()),
  });
  const loadFor = (userId: string) => workload.data?.find((w) => w.userId === userId);

  const myRole = workspace.data?.role ?? 'member';
  const canManage = myRole !== 'member';

  return (
    <section className="members-page">
      <header className="page-header">
        <div>
          <Link to={`/w/${workspaceId}`} className="muted">
            ← {workspace.data?.name ?? 'Workspace'}
          </Link>
          <h1>Members</h1>
        </div>
      </header>
      <ErrorMessage error={workspace.error ?? members.error} />
      <ul className="member-list">
        {members.data?.map((member) => (
          <MemberRow
            key={member.userId}
            workspaceId={workspaceId}
            member={member}
            myRole={myRole}
            load={loadFor(member.userId)}
          />
        ))}
      </ul>
      {canManage && <Invitations workspaceId={workspaceId} myRole={myRole} />}
      {canManage && workspace.data && (
        <WorkspaceSettings workspaceId={workspaceId} name={workspace.data.name} myRole={myRole} />
      )}
    </section>
  );
}

function WorkspaceSettings({
  workspaceId,
  name: currentName,
  myRole,
}: {
  workspaceId: string;
  name: string;
  myRole: Role;
}) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState(currentName);
  const rename = useMutation({
    mutationFn: () => workspacesApi.rename(workspaceId, name),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces });
    },
  });
  const remove = useMutation({
    mutationFn: () => workspacesApi.remove(workspaceId),
    onSuccess: () => {
      queryClient.removeQueries({ queryKey: queryKeys.workspace(workspaceId) });
      queryClient.invalidateQueries({ queryKey: queryKeys.workspaces });
      navigate('/', { replace: true });
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    rename.mutate();
  }

  return (
    <section className="workspace-settings">
      <h2>Workspace</h2>
      <form className="invite-form" onSubmit={submit}>
        <input
          aria-label="Workspace name"
          value={name}
          maxLength={80}
          onChange={(e) => setName(e.target.value)}
          required
        />
        <button disabled={rename.isPending || name.trim() === currentName}>Rename</button>
      </form>
      <ErrorMessage error={rename.error ?? remove.error} />
      {myRole === 'owner' && (
        <div className="danger-zone">
          <h3>Delete workspace</h3>
          <p className="muted small">
            Deletes every board, card and comment in it for everyone. This can't be undone.
          </p>
          <button
            type="button"
            className="danger"
            disabled={remove.isPending}
            onClick={() => {
              const typed = window.prompt(`Type the workspace name (${currentName}) to delete it:`);
              if (typed?.trim() === currentName) {
                remove.mutate();
              }
            }}
          >
            Delete workspace
          </button>
        </div>
      )}
    </section>
  );
}

function MemberRow({
  workspaceId,
  member,
  myRole,
  load,
}: {
  workspaceId: string;
  member: Member;
  myRole: Role;
  load?: Workload;
}) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const isMe = user?.id === member.userId;

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.members(workspaceId) });
    queryClient.invalidateQueries({ queryKey: queryKeys.workspace(workspaceId) });
  };
  const setRole = useMutation({
    mutationFn: (role: Role) => workspacesApi.setRole(workspaceId, member.userId, role),
    onSuccess: refresh,
  });
  const remove = useMutation({
    mutationFn: () => workspacesApi.removeMember(workspaceId, member.userId),
    onSuccess: () => {
      if (isMe) {
        queryClient.invalidateQueries({ queryKey: queryKeys.workspaces });
        navigate('/');
      } else {
        refresh();
      }
    },
  });

  // Owners can change anyone; admins can change members and admins.
  const canChangeRole = myRole === 'owner' || (myRole === 'admin' && member.role !== 'owner');
  const roleOptions: Role[] =
    myRole === 'owner' ? ['owner', 'admin', 'member'] : ['admin', 'member'];
  const canRemove = isMe || canChangeRole;

  return (
    <li className="member">
      <Avatar id={member.userId} name={member.displayName} size={32} />
      <div className="member-info">
        <strong>
          {member.displayName}
          {isMe && ' (you)'}
        </strong>
        <span className="muted small">{member.email}</span>
      </div>
      {load && (
        <span className="workload small" aria-label={`Workload for ${member.displayName}`}>
          {load.open} open
          {load.overdue > 0 && (
            <span className="due-badge due-overdue">{load.overdue} overdue</span>
          )}
          {load.dueThisWeek > 0 && <span className="muted"> · {load.dueThisWeek} this week</span>}
        </span>
      )}
      {canChangeRole ? (
        <select
          aria-label={`Role for ${member.displayName}`}
          value={member.role}
          disabled={setRole.isPending}
          onChange={(e) => setRole.mutate(e.target.value as Role)}
        >
          {roleOptions.map((role) => (
            <option key={role} value={role}>
              {ROLE_LABELS[role]}
            </option>
          ))}
        </select>
      ) : (
        <span className="role-badge">{ROLE_LABELS[member.role]}</span>
      )}
      {canRemove && (
        <button
          type="button"
          className="danger"
          onClick={() =>
            window.confirm(
              isMe ? 'Leave this workspace?' : `Remove ${member.displayName} from the workspace?`,
            ) && remove.mutate()
          }
        >
          {isMe ? 'Leave' : 'Remove'}
        </button>
      )}
      <ErrorMessage error={setRole.error ?? remove.error} />
    </li>
  );
}

function Invitations({ workspaceId, myRole }: { workspaceId: string; myRole: Role }) {
  const queryClient = useQueryClient();
  const [email, setEmail] = useState('');
  const [role, setRole] = useState<Role>('member');
  const [created, setCreated] = useState<CreatedInvitation | null>(null);
  const [copied, setCopied] = useState<boolean | null>(null);

  const invitations = useQuery({
    queryKey: queryKeys.invitations(workspaceId),
    queryFn: () => workspacesApi.invitations(workspaceId),
  });
  const invite = useMutation({
    mutationFn: () => workspacesApi.invite(workspaceId, email, role),
    onSuccess: (invitation) => {
      setCreated(invitation);
      setCopied(null);
      setEmail('');
      queryClient.invalidateQueries({ queryKey: queryKeys.invitations(workspaceId) });
    },
  });
  const revoke = useMutation({
    mutationFn: (id: string) => workspacesApi.revokeInvitation(workspaceId, id),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: queryKeys.invitations(workspaceId) }),
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    invite.mutate();
  }

  const link = created ? new URL(`/invite/${created.token}`, window.location.origin).href : '';

  return (
    <section className="invitations">
      <h2>Invite people</h2>
      <form className="invite-form" onSubmit={submit}>
        <input
          type="email"
          aria-label="Email address"
          placeholder="teammate@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
        />
        <select aria-label="Role" value={role} onChange={(e) => setRole(e.target.value as Role)}>
          <option value="member">Member</option>
          {myRole !== 'member' && <option value="admin">Admin</option>}
        </select>
        <button className="primary" disabled={invite.isPending}>
          Send invite
        </button>
      </form>
      <ErrorMessage error={invite.error} />
      {created && (
        <div className="invite-link" role="status">
          <p>
            Invitation created for <strong>{created.email}</strong>. Send them this link (it works
            for 7 days):
          </p>
          <div className="row">
            <input
              aria-label="Invitation link"
              readOnly
              value={link}
              onFocus={(e) => e.target.select()}
            />
            <button type="button" onClick={() => copyLink(link).then(setCopied)}>
              {copied ? 'Copied' : 'Copy'}
            </button>
          </div>
          {copied === false && (
            <p className="muted small">Couldn't copy automatically. Press Ctrl+C (⌘C on a Mac).</p>
          )}
        </div>
      )}

      {invitations.data && invitations.data.length > 0 && (
        <>
          <h3>Pending invitations</h3>
          <ul className="member-list">
            {invitations.data.map((inv) => (
              <li key={inv.id} className="member">
                <div className="member-info">
                  <strong>{inv.email}</strong>
                  <span className="muted small">
                    {ROLE_LABELS[inv.role]} · invited by {inv.invitedBy}{' '}
                    {formatRelative(inv.createdAt)}
                  </span>
                </div>
                <button type="button" onClick={() => revoke.mutate(inv.id)}>
                  Cancel invite
                </button>
              </li>
            ))}
          </ul>
        </>
      )}
      <ErrorMessage error={revoke.error ?? invitations.error} />
    </section>
  );
}
