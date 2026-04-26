import { useMutation, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { queryKeys } from '../lib/queryKeys';

interface StarButtonProps {
  boardId: string;
  workspaceId: string;
  starred: boolean;
}

export function StarButton({ boardId, workspaceId, starred }: StarButtonProps) {
  const queryClient = useQueryClient();
  const toggle = useMutation({
    mutationFn: () => boardsApi.star(boardId, !starred),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId) });
      queryClient.invalidateQueries({ queryKey: ['workspaces', workspaceId, 'boards'] });
    },
  });

  return (
    <button
      type="button"
      className={starred ? 'star-button starred' : 'star-button'}
      aria-pressed={starred}
      aria-label={starred ? 'Unstar board' : 'Star board'}
      title={starred ? 'Unstar board' : 'Star board'}
      disabled={toggle.isPending}
      onClick={(e) => {
        e.preventDefault();
        e.stopPropagation();
        toggle.mutate();
      }}
    >
      {starred ? '★' : '☆'}
    </button>
  );
}
