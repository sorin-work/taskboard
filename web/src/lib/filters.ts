import type { CardFilters } from '../api/boards';

const KEYS = ['assignee', 'label', 'due', 'q'] as const;

export const DUE_OPTIONS = [
  { value: 'overdue', label: 'Overdue' },
  { value: 'today', label: 'Due today' },
  { value: 'week', label: 'Due this week' },
  { value: 'none', label: 'No due date' },
] as const;

export function readFilters(params: URLSearchParams): CardFilters {
  const filters: CardFilters = {};
  for (const key of KEYS) {
    const value = params.get(key)?.trim();
    if (value) {
      filters[key] = value;
    }
  }
  return filters;
}

/** Returns new search params with the filters applied, keeping anything else (like ?card=). */
export function writeFilters(params: URLSearchParams, filters: CardFilters): URLSearchParams {
  const next = new URLSearchParams(params);
  for (const key of KEYS) {
    const value = filters[key]?.trim();
    if (value) {
      next.set(key, value);
    } else {
      next.delete(key);
    }
  }
  return next;
}

export function activeFilterCount(filters: CardFilters): number {
  return KEYS.filter((key) => filters[key]).length;
}
