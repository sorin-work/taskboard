import { MutationCache, QueryClient } from '@tanstack/react-query';
import { ApiError } from './api/client';

export function createQueryClient() {
  const queryClient: QueryClient = new QueryClient({
    // Almost any change (completing, assigning, moving, deleting a card) can
    // change what's in "my tasks" and the overdue badge, so refresh them
    // after every successful mutation rather than remembering it everywhere.
    mutationCache: new MutationCache({
      onSuccess: () => queryClient.invalidateQueries({ queryKey: ['me', 'tasks'] }),
    }),
    defaultOptions: {
      queries: {
        staleTime: 15_000,
        refetchOnWindowFocus: false,
        // Retrying a 403/404 just delays the error message.
        retry: (failureCount, error) =>
          !(error instanceof ApiError && error.status >= 400 && error.status < 500) &&
          failureCount < 2,
      },
    },
  });
  return queryClient;
}
