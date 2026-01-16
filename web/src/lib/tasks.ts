import type { MyTask } from '../api/types';
import { dueStatus } from './dates';

export interface TaskGroup {
  key: string;
  title: string;
  tasks: MyTask[];
}

/** Buckets tasks the way people think about them: late, today, this week, later, someday. */
export function groupTasks(tasks: MyTask[], today: string): TaskGroup[] {
  const groups: TaskGroup[] = [
    { key: 'overdue', title: 'Overdue', tasks: [] },
    { key: 'today', title: 'Today', tasks: [] },
    { key: 'soon', title: 'Next few days', tasks: [] },
    { key: 'later', title: 'Later', tasks: [] },
    { key: 'none', title: 'No due date', tasks: [] },
    { key: 'done', title: 'Completed', tasks: [] },
  ];
  for (const task of tasks) {
    const status = dueStatus(task.dueDate, task.completed, today) ?? 'none';
    const target = task.completed ? 'done' : status;
    groups.find((g) => g.key === target)!.tasks.push(task);
  }
  return groups.filter((g) => g.tasks.length > 0);
}
