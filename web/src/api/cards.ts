import { api } from './client';
import type { Activity, Card, Checklist, ChecklistItem, Comment } from './types';

export interface CardUpdate {
  title: string;
  description: string;
  dueDate: string | null;
  completed: boolean;
}

export const cardsApi = {
  create: (columnId: string, title: string) =>
    api<Card>(`/columns/${columnId}/cards`, { method: 'POST', body: { title } }),
  get: (cardId: string) => api<Card>(`/cards/${cardId}`),
  update: (cardId: string, body: CardUpdate) =>
    api<Card>(`/cards/${cardId}`, { method: 'PUT', body }),
  remove: (cardId: string) => api<void>(`/cards/${cardId}`, { method: 'DELETE' }),
  duplicate: (cardId: string) => api<Card>(`/cards/${cardId}/duplicate`, { method: 'POST' }),
  archive: (cardId: string) => api<Card>(`/cards/${cardId}/archive`, { method: 'POST' }),
  restore: (cardId: string) => api<Card>(`/cards/${cardId}/restore`, { method: 'POST' }),
  move: (cardId: string, columnId: string, afterCardId: string | null) =>
    api<Card>(`/cards/${cardId}/move`, { method: 'POST', body: { columnId, afterCardId } }),
  setLabels: (cardId: string, labelIds: string[]) =>
    api<Card>(`/cards/${cardId}/labels`, { method: 'PUT', body: { labelIds } }),
  setAssignees: (cardId: string, userIds: string[]) =>
    api<Card>(`/cards/${cardId}/assignees`, { method: 'PUT', body: { userIds } }),

  addChecklist: (cardId: string, title: string) =>
    api<Checklist>(`/cards/${cardId}/checklists`, { method: 'POST', body: { title } }),
  renameChecklist: (checklistId: string, title: string) =>
    api<Checklist>(`/checklists/${checklistId}`, { method: 'PATCH', body: { title } }),
  deleteChecklist: (checklistId: string) =>
    api<void>(`/checklists/${checklistId}`, { method: 'DELETE' }),
  addItem: (checklistId: string, text: string) =>
    api<ChecklistItem>(`/checklists/${checklistId}/items`, { method: 'POST', body: { text } }),
  updateItem: (itemId: string, text: string, done: boolean) =>
    api<ChecklistItem>(`/checklist-items/${itemId}`, { method: 'PATCH', body: { text, done } }),
  moveItem: (itemId: string, position: number) =>
    api<Checklist>(`/checklist-items/${itemId}/move`, { method: 'POST', body: { position } }),
  deleteItem: (itemId: string) => api<void>(`/checklist-items/${itemId}`, { method: 'DELETE' }),

  comments: (cardId: string) => api<Comment[]>(`/cards/${cardId}/comments`),
  addComment: (cardId: string, body: string) =>
    api<Comment>(`/cards/${cardId}/comments`, { method: 'POST', body: { body } }),
  editComment: (commentId: string, body: string) =>
    api<Comment>(`/comments/${commentId}`, { method: 'PATCH', body: { body } }),
  deleteComment: (commentId: string) => api<void>(`/comments/${commentId}`, { method: 'DELETE' }),

  activity: (cardId: string) => api<Activity[]>(`/cards/${cardId}/activity`),
};
