import { describe, expect, it } from 'vitest';
import { safeNext } from './navigation';

describe('safeNext', () => {
  it.each([
    ['/boards/1', '/boards/1'],
    ['/invite/abc?x=1', '/invite/abc?x=1'],
    [null, '/'],
    ['', '/'],
    ['https://evil.example', '/'],
    ['//evil.example', '/'],
  ])('%s -> %s', (input, expected) => {
    expect(safeNext(input)).toBe(expected);
  });
});
