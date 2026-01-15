import { describe, expect, it } from 'vitest';
import { activeFilterCount, readFilters, writeFilters } from './filters';

describe('filters', () => {
  it('reads known keys and ignores blanks', () => {
    const params = new URLSearchParams('assignee=me&label=&due=overdue&card=c1&q=%20');
    expect(readFilters(params)).toEqual({ assignee: 'me', due: 'overdue' });
  });

  it('writes filters and keeps other params', () => {
    const params = new URLSearchParams('card=c1&due=today');
    const next = writeFilters(params, { label: 'l1', due: undefined });
    expect(next.get('card')).toBe('c1');
    expect(next.get('label')).toBe('l1');
    expect(next.has('due')).toBe(false);
  });

  it('counts active filters', () => {
    expect(activeFilterCount({})).toBe(0);
    expect(activeFilterCount({ assignee: 'me', q: 'login' })).toBe(2);
  });
});
