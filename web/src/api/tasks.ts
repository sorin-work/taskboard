import { todayIso } from '../lib/dates';
import { api } from './client';
import type { MyTask } from './types';

export const tasksApi = {
  mine: (params: { due?: string; includeCompleted?: boolean } = {}) => {
    const search = new URLSearchParams();
    if (params.due) search.set('due', params.due);
    if (params.includeCompleted) search.set('includeCompleted', 'true');
    search.set('today', todayIso());
    const query = search.toString();
    return api<MyTask[]>(`/me/tasks${query ? `?${query}` : ''}`);
  },
};
