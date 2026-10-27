import type { ReactNode } from 'react';
import { renderHook } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it, vi } from 'vitest';
import { useRefreshCard } from './useRefreshCard';

describe('useRefreshCard', () => {
  it('refreshes the card, the board cards and the board activity', () => {
    const client = new QueryClient();
    const spy = vi.spyOn(client, 'invalidateQueries');
    const wrapper = ({ children }: { children: ReactNode }) => (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );

    const { result } = renderHook(() => useRefreshCard('b1', 'c1'), { wrapper });
    result.current();

    const keys = spy.mock.calls.map(([filters]) => filters?.queryKey);
    expect(keys).toEqual([
      ['cards', 'c1'],
      ['boards', 'b1', 'cards'],
      ['boards', 'b1', 'activity'],
    ]);
  });
});
