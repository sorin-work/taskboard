import { api } from './client';
import type {
  CreatedInvitation,
  Invitation,
  InvitationPreview,
  Member,
  Role,
  SearchResult,
  Workspace,
} from './types';

export const workspacesApi = {
  list: () => api<Workspace[]>('/workspaces'),
  get: (id: string) => api<Workspace>(`/workspaces/${id}`),
  create: (name: string) => api<Workspace>('/workspaces', { method: 'POST', body: { name } }),
  rename: (id: string, name: string) =>
    api<Workspace>(`/workspaces/${id}`, { method: 'PATCH', body: { name } }),
  remove: (id: string) => api<void>(`/workspaces/${id}`, { method: 'DELETE' }),
  search: (id: string, q: string) =>
    api<SearchResult[]>(`/workspaces/${id}/search?q=${encodeURIComponent(q)}`),

  members: (id: string) => api<Member[]>(`/workspaces/${id}/members`),
  setRole: (id: string, userId: string, role: Role) =>
    api<Member>(`/workspaces/${id}/members/${userId}`, { method: 'PATCH', body: { role } }),
  removeMember: (id: string, userId: string) =>
    api<void>(`/workspaces/${id}/members/${userId}`, { method: 'DELETE' }),

  invitations: (id: string) => api<Invitation[]>(`/workspaces/${id}/invitations`),
  invite: (id: string, email: string, role: Role) =>
    api<CreatedInvitation>(`/workspaces/${id}/invitations`, {
      method: 'POST',
      body: { email, role },
    }),
  revokeInvitation: (id: string, invitationId: string) =>
    api<void>(`/workspaces/${id}/invitations/${invitationId}`, { method: 'DELETE' }),
};

export const invitationsApi = {
  preview: (token: string) => api<InvitationPreview>(`/invitations/${encodeURIComponent(token)}`),
  accept: (token: string) =>
    api<Workspace>(`/invitations/${encodeURIComponent(token)}/accept`, { method: 'POST' }),
};
