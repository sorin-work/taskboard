export type Role = 'owner' | 'admin' | 'member';

export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface Workspace {
  id: string;
  name: string;
  role: Role;
  memberCount: number;
  createdAt: string;
}

export interface Member {
  userId: string;
  email: string;
  displayName: string;
  role: Role;
  joinedAt: string;
}

export interface Invitation {
  id: string;
  email: string;
  role: Role;
  invitedBy: string;
  createdAt: string;
  expiresAt: string;
}

export interface CreatedInvitation extends Invitation {
  token: string;
  acceptUrl: string;
}

export type InvitationStatus = 'pending' | 'accepted' | 'expired' | 'revoked';

export interface InvitationPreview {
  workspaceId: string;
  workspaceName: string;
  invitedBy: string;
  email: string;
  role: Role;
  expiresAt: string;
  status: InvitationStatus;
}

export interface BoardSummary {
  id: string;
  workspaceId: string;
  name: string;
  description: string;
  cardCount: number;
  overdueCount: number;
  createdAt: string;
  archivedAt: string | null;
  starred: boolean;
}

export interface Column {
  id: string;
  name: string;
  position: number;
  wipLimit: number | null;
}

export type LabelColor =
  | 'gray'
  | 'red'
  | 'orange'
  | 'yellow'
  | 'green'
  | 'teal'
  | 'blue'
  | 'purple'
  | 'pink';

export interface Label {
  id: string;
  name: string;
  color: LabelColor;
}

export interface BoardMember {
  userId: string;
  displayName: string;
  email: string;
}

export interface Board {
  id: string;
  workspaceId: string;
  workspaceName: string;
  name: string;
  description: string;
  role: Role;
  createdAt: string;
  archivedAt: string | null;
  starred: boolean;
  columns: Column[];
  labels: Label[];
  members: BoardMember[];
}

export interface CardSummary {
  id: string;
  columnId: string;
  title: string;
  position: number;
  dueDate: string | null;
  completed: boolean;
  assigneeIds: string[];
  labelIds: string[];
  checklistDone: number;
  checklistTotal: number;
  commentCount: number;
}

export interface CardUser {
  id: string;
  displayName: string;
}

export interface ChecklistItem {
  id: string;
  text: string;
  done: boolean;
  position: number;
}

export interface Checklist {
  id: string;
  title: string;
  items: ChecklistItem[];
}

export interface Card {
  id: string;
  boardId: string;
  columnId: string;
  columnName: string;
  title: string;
  description: string;
  position: number;
  dueDate: string | null;
  completedAt: string | null;
  archivedAt: string | null;
  createdBy: CardUser;
  assignees: CardUser[];
  labels: Label[];
  checklists: Checklist[];
  createdAt: string;
  updatedAt: string;
}

export interface Comment {
  id: string;
  cardId: string;
  author: CardUser;
  body: string;
  createdAt: string;
  editedAt: string | null;
}

export interface Activity {
  id: string;
  type: string;
  cardId: string | null;
  cardTitle: string | null;
  actor: CardUser;
  data: Record<string, string>;
  createdAt: string;
}

export interface MyTask {
  id: string;
  title: string;
  dueDate: string | null;
  completed: boolean;
  boardId: string;
  boardName: string;
  workspaceId: string;
  workspaceName: string;
  columnName: string;
  labels: Label[];
}

export interface SearchResult {
  cardId: string;
  title: string;
  snippet: string | null;
  boardId: string;
  boardName: string;
  columnName: string;
  dueDate: string | null;
  completed: boolean;
}
