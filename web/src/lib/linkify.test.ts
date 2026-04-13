import { describe, expect, it } from 'vitest';
import { linkify } from './linkify';

describe('linkify', () => {
  it('leaves plain text alone', () => {
    expect(linkify('no links here')).toEqual([{ kind: 'text', text: 'no links here' }]);
  });

  it('finds links in the middle of text', () => {
    expect(linkify('see https://example.com/a?b=1 for details')).toEqual([
      { kind: 'text', text: 'see ' },
      { kind: 'link', text: 'https://example.com/a?b=1', href: 'https://example.com/a?b=1' },
      { kind: 'text', text: ' for details' },
    ]);
  });

  it('drops sentence punctuation after a link', () => {
    expect(linkify('Spec: https://example.com/spec.')).toEqual([
      { kind: 'text', text: 'Spec: ' },
      { kind: 'link', text: 'https://example.com/spec', href: 'https://example.com/spec' },
      { kind: 'text', text: '.' },
    ]);
  });

  it('keeps balanced parentheses', () => {
    const url = 'https://en.wikipedia.org/wiki/Kanban_(development)';
    expect(linkify(`(${url})`)[1]).toEqual({ kind: 'link', text: url, href: url });
  });

  it('ignores other schemes', () => {
    expect(linkify('javascript:alert(1)')).toEqual([{ kind: 'text', text: 'javascript:alert(1)' }]);
  });
});
