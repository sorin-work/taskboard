import { screen, within } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { MyTask } from '../api/types';
import { addDays, todayIso } from '../lib/dates';
import { groupTasks } from '../lib/tasks';
import { mockApi, renderWithProviders } from '../test/utils';
import { MyTasksPage } from './MyTasksPage';

const today = todayIso();

function task(id: string, dueDate: string | null, completed = false): MyTask {
  return {
    id,
    title: `Task ${id}`,
    dueDate,
    completed,
    boardId: 'b1',
    boardName: 'Website',
    workspaceId: 'w1',
    workspaceName: 'Studio',
    columnName: 'To do',
    labels: [],
  };
}

describe('groupTasks', () => {
  it('puts tasks in due date buckets', () => {
    const groups = groupTasks(
      [
        task('late', addDays(today, -3)),
        task('now', today),
        task('soon', addDays(today, 2)),
        task('later', addDays(today, 10)),
        task('someday', null),
        task('done', addDays(today, -3), true),
      ],
      today,
    );

    expect(groups.map((g) => [g.title, g.tasks.map((t) => t.id)])).toEqual([
      ['Overdue', ['late']],
      ['Today', ['now']],
      ['Next few days', ['soon']],
      ['Later', ['later']],
      ['No due date', ['someday']],
      ['Completed', ['done']],
    ]);
  });
});

describe('MyTasksPage', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('lists tasks with a link to the card', async () => {
    mockApi({ 'GET /api/me/tasks': () => ({ body: [task('1', today)] }) });
    renderWithProviders(<MyTasksPage />);

    const link = await screen.findByRole('link', { name: 'Task 1' });
    expect(link).toHaveAttribute('href', '/b/b1?card=1');
    const group = screen.getByRole('heading', { name: /Today/ }).parentElement!;
    expect(within(group).getByText('Studio › Website › To do')).toBeInTheDocument();
  });

  it('shows an empty state', async () => {
    mockApi({ 'GET /api/me/tasks': () => ({ body: [] }) });
    renderWithProviders(<MyTasksPage />);

    expect(await screen.findByText(/Nothing assigned to you/)).toBeInTheDocument();
  });
});
