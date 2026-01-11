/** Today's date in the browser's time zone, as YYYY-MM-DD. */
export function todayIso(now: Date = new Date()): string {
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export function addDays(iso: string, days: number): string {
  const date = new Date(`${iso}T12:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

export type DueStatus = 'done' | 'overdue' | 'today' | 'soon' | 'later';

/** How urgent a due date is. "soon" means within the next two days. */
export function dueStatus(
  dueDate: string | null,
  completed: boolean,
  today: string,
): DueStatus | null {
  if (!dueDate) {
    return null;
  }
  if (completed) {
    return 'done';
  }
  if (dueDate < today) {
    return 'overdue';
  }
  if (dueDate === today) {
    return 'today';
  }
  return dueDate <= addDays(today, 2) ? 'soon' : 'later';
}

export function formatDue(dueDate: string, today: string): string {
  if (dueDate === today) {
    return 'Today';
  }
  if (dueDate === addDays(today, 1)) {
    return 'Tomorrow';
  }
  if (dueDate === addDays(today, -1)) {
    return 'Yesterday';
  }
  const date = new Date(dueDate);
  const sameYear = dueDate.slice(0, 4) === today.slice(0, 4);
  return date.toLocaleDateString('en', {
    month: 'short',
    day: 'numeric',
    year: sameYear ? undefined : 'numeric',
  });
}

/** "just now", "5 min ago", "3 h ago", "yesterday", or a date. */
export function formatRelative(iso: string, now: Date = new Date()): string {
  const then = new Date(iso);
  const seconds = Math.round((now.getTime() - then.getTime()) / 1000);
  if (seconds < 60) {
    return 'just now';
  }
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) {
    return `${minutes} min ago`;
  }
  const hours = Math.floor(minutes / 60);
  if (hours < 24) {
    return `${hours} h ago`;
  }
  if (todayIso(then) === addDays(todayIso(now), -1)) {
    return 'yesterday';
  }
  return then.toLocaleDateString('en', {
    month: 'short',
    day: 'numeric',
    year: then.getFullYear() === now.getFullYear() ? undefined : 'numeric',
  });
}
