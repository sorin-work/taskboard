import { describe, expect, it } from 'vitest';
import { collapsedColumns } from './collapsedColumns';

describe('collapsedColumns', () => {
  it('toggles columns per board', () => {
    expect(collapsedColumns.toggle('b1', 'done')).toEqual(['done']);
    expect(collapsedColumns.toggle('b1', 'backlog')).toEqual(['done', 'backlog']);
    expect(collapsedColumns.get('b2')).toEqual([]);
    expect(collapsedColumns.toggle('b1', 'done')).toEqual(['backlog']);
  });

  it('ignores junk in storage', () => {
    localStorage.setItem('taskboard.collapsed.b3', '{"not":"a list"}');
    expect(collapsedColumns.get('b3')).toEqual([]);
    localStorage.setItem('taskboard.collapsed.b3', 'not json');
    expect(collapsedColumns.get('b3')).toEqual([]);
  });
});
