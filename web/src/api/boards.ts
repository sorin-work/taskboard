import { api } from './client';
import type {
  Activity,
  Board,
  BoardSummary,
  CardSummary,
  Column,
  Label,
  LabelColor,
} from './types';

export interface CardFilters {
  assignee?: string;
  label?: string;
  due?: string;
  q?: string;
}

function toQuery(params: Record<string, string | undefined>) {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value) {
      search.set(key, value);
    }
  }
  const text = search.toString();
  return text ? `?${text}` : '';
}

export const boardsApi = {
  list: (workspaceId: string, includeArchived = false) =>
    api<BoardSummary[]>(
      `/workspaces/${workspaceId}/boards${includeArchived ? '?includeArchived=true' : ''}`,
    ),
  create: (workspaceId: string, name: string, description?: string) =>
    api<Board>(`/workspaces/${workspaceId}/boards`, {
      method: 'POST',
      body: { name, description },
    }),
  get: (boardId: string) => api<Board>(`/boards/${boardId}`),
  update: (boardId: string, body: { name: string; description: string; archived: boolean }) =>
    api<Board>(`/boards/${boardId}`, { method: 'PATCH', body }),
  remove: (boardId: string) => api<void>(`/boards/${boardId}`, { method: 'DELETE' }),

  cards: (boardId: string, filters: CardFilters = {}) =>
    api<CardSummary[]>(`/boards/${boardId}/cards${toQuery({ ...filters })}`),
  activity: (boardId: string, before?: string) =>
    api<Activity[]>(`/boards/${boardId}/activity${toQuery({ before })}`),

  addColumn: (boardId: string, name: string) =>
    api<Column>(`/boards/${boardId}/columns`, { method: 'POST', body: { name } }),
  renameColumn: (columnId: string, name: string) =>
    api<Column>(`/columns/${columnId}`, { method: 'PATCH', body: { name } }),
  moveColumn: (columnId: string, position: number) =>
    api<Column[]>(`/columns/${columnId}/move`, { method: 'POST', body: { position } }),
  setWipLimit: (columnId: string, limit: number | null) =>
    api<Column>(`/columns/${columnId}/wip-limit`, { method: 'PUT', body: { limit } }),
  deleteColumn: (columnId: string) => api<void>(`/columns/${columnId}`, { method: 'DELETE' }),

  createLabel: (boardId: string, name: string, color: LabelColor) =>
    api<Label>(`/boards/${boardId}/labels`, { method: 'POST', body: { name, color } }),
  updateLabel: (labelId: string, name: string, color: LabelColor) =>
    api<Label>(`/labels/${labelId}`, { method: 'PATCH', body: { name, color } }),
  deleteLabel: (labelId: string) => api<void>(`/labels/${labelId}`, { method: 'DELETE' }),
};
