import { useInfiniteQuery } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';
import { ActivityList } from './ActivityList';

const PAGE_SIZE = 30;

interface BoardActivityPanelProps {
  boardId: string;
  onClose: () => void;
  onOpenCard: (cardId: string) => void;
}

export function BoardActivityPanel({ boardId, onClose, onOpenCard }: BoardActivityPanelProps) {
  const activity = useInfiniteQuery({
    queryKey: queryKeys.boardActivity(boardId),
    queryFn: ({ pageParam }) => boardsApi.activity(boardId, pageParam),
    initialPageParam: undefined as { createdAt: string; id: string } | undefined,
    getNextPageParam: (lastPage) => {
      const last = lastPage.at(-1);
      return lastPage.length < PAGE_SIZE || !last
        ? undefined
        : { createdAt: last.createdAt, id: last.id };
    },
  });

  return (
    <aside className="side-panel" aria-label="Board activity">
      <header>
        <h2>Activity</h2>
        <button type="button" className="icon-button" aria-label="Close activity" onClick={onClose}>
          ×
        </button>
      </header>
      <ErrorMessage error={activity.error} />
      {activity.data && <ActivityList items={activity.data.pages.flat()} onOpenCard={onOpenCard} />}
      {activity.hasNextPage && (
        <button
          type="button"
          onClick={() => activity.fetchNextPage()}
          disabled={activity.isFetchingNextPage}
        >
          {activity.isFetchingNextPage ? 'Loading…' : 'Show older'}
        </button>
      )}
    </aside>
  );
}
