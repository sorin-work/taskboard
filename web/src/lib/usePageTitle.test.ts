import { renderHook } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { usePageTitle } from './usePageTitle';

describe('usePageTitle', () => {
  it('sets and resets the document title', () => {
    const { rerender, unmount } = renderHook(({ title }) => usePageTitle(title), {
      initialProps: { title: undefined as string | undefined },
    });
    expect(document.title).toBe('Taskboard');

    rerender({ title: 'Website relaunch' });
    expect(document.title).toBe('Website relaunch · Taskboard');

    unmount();
    expect(document.title).toBe('Taskboard');
  });
});
