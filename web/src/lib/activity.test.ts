import { describe, expect, it } from 'vitest';
import type { Activity } from '../api/types';
import { describeActivity } from './activity';

function entry(type: string, data: Record<string, string>, cardTitle: string | null = 'Ship v2') {
  return {
    id: 'a1',
    type,
    cardId: cardTitle ? 'c1' : null,
    cardTitle,
    actor: { id: 'u1', displayName: 'Dana' },
    data,
    createdAt: '2026-01-10T10:00:00Z',
  } satisfies Activity;
}

describe('describeActivity', () => {
  it('describes moves with the column names', () => {
    expect(describeActivity(entry('card.moved', { from: 'To do', to: 'Done' }))).toBe(
      'moved "Ship v2" from To do to Done',
    );
  });

  it('says "this card" inside a card feed', () => {
    expect(describeActivity(entry('card.completed', {}), true)).toBe(
      'marked this card as complete',
    );
    expect(describeActivity(entry('card.created', { column: 'Backlog' }), true)).toBe(
      'created this card in Backlog',
    );
  });

  it('falls back to the stored title for deleted cards', () => {
    expect(describeActivity(entry('card.deleted', { title: 'Old idea' }, null))).toBe(
      'deleted "Old idea"',
    );
  });

  it('describes board level events', () => {
    expect(
      describeActivity(entry('column.renamed', { from: 'Doing', column: 'In progress' }, null)),
    ).toBe('renamed the column "Doing" to "In progress"');
  });

  it('includes comment excerpts', () => {
    expect(describeActivity(entry('comment.added', { excerpt: 'Looks good' }))).toBe(
      'commented on "Ship v2": “Looks good”',
    );
  });

  it('handles types it does not know', () => {
    expect(describeActivity(entry('card.teleported', {}))).toBe('changed "Ship v2"');
  });
});
