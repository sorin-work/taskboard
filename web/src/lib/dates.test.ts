import { describe, expect, it } from 'vitest';
import { addDays, dueStatus, formatDue, formatRelative, todayIso } from './dates';

describe('todayIso', () => {
  it('uses local date parts', () => {
    expect(todayIso(new Date(2026, 0, 5, 23, 30))).toBe('2026-01-05');
  });
});

describe('addDays', () => {
  it('crosses month and year boundaries', () => {
    expect(addDays('2025-12-31', 1)).toBe('2026-01-01');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });
});

describe('dueStatus', () => {
  const today = '2026-01-10';

  it.each([
    [null, false, null],
    ['2026-01-09', true, 'done'],
    ['2026-01-09', false, 'overdue'],
    ['2026-01-10', false, 'today'],
    ['2026-01-12', false, 'soon'],
    ['2026-01-13', false, 'later'],
  ])('%s (completed: %s) -> %s', (due, completed, expected) => {
    expect(dueStatus(due, completed, today)).toBe(expected);
  });
});

describe('formatDue', () => {
  const today = '2026-01-10';

  it('names nearby days', () => {
    expect(formatDue('2026-01-10', today)).toBe('Today');
    expect(formatDue('2026-01-11', today)).toBe('Tomorrow');
    expect(formatDue('2026-01-09', today)).toBe('Yesterday');
  });

  it('counts days for the coming week', () => {
    expect(formatDue('2026-01-12', today)).toBe('In 2 days');
    expect(formatDue('2026-01-16', today)).toBe('In 6 days');
    expect(formatDue('2026-01-17', today)).toBe('Jan 17');
  });

  it('shows the same calendar day in every time zone', () => {
    expect(formatDue('2026-02-03', today)).toBe('Feb 3');
  });

  it('adds the year only when it differs', () => {
    expect(formatDue('2027-02-03', today)).toBe('Feb 3, 2027');
  });
});

describe('formatRelative', () => {
  const now = new Date(2026, 0, 10, 12, 0, 0);

  it.each([
    [new Date(2026, 0, 10, 11, 59, 30), 'just now'],
    [new Date(2026, 0, 10, 11, 45), '15 min ago'],
    [new Date(2026, 0, 10, 9, 0), '3 h ago'],
    [new Date(2026, 0, 9, 8, 0), 'yesterday'],
  ])('%s -> %s', (then, expected) => {
    expect(formatRelative(then.toISOString(), now)).toBe(expected);
  });
});
