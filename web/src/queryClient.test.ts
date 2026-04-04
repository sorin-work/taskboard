import { describe, expect, it, vi } from 'vitest';
import { ApiError } from './api/client';
import { createQueryClient } from './queryClient';

describe('createQueryClient', () => {
  it('refreshes my tasks after any successful mutation', async () => {
    const client = createQueryClient();
    const spy = vi.spyOn(client, 'invalidateQueries');

    await client
      .getMutationCache()
      .build(client, { mutationFn: async () => 'done' })
      .execute(undefined);

    expect(spy).toHaveBeenCalledWith({ queryKey: ['me', 'tasks'] });
  });

  it('does not retry client errors', () => {
    const retry = createQueryClient().getDefaultOptions().queries!.retry as (
      count: number,
      error: unknown,
    ) => boolean;

    expect(retry(0, new ApiError(404, 'Not found'))).toBe(false);
    expect(retry(0, new ApiError(503, 'Unavailable'))).toBe(true);
    expect(retry(2, new ApiError(503, 'Unavailable'))).toBe(false);
  });
});
