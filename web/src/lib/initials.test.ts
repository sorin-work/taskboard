import { describe, expect, it } from 'vitest';
import { initials } from './initials';

describe('initials', () => {
  it.each([
    ['Dana Scully', 'DS'],
    ['Ana María López', 'AL'],
    ['prince', 'P'],
    ['  ', '?'],
  ])('%s -> %s', (name, expected) => {
    expect(initials(name)).toBe(expected);
  });
});
