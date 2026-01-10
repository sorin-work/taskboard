import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

export function AddColumnForm({ boardId }: { boardId: string }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState('');
  const add = useMutation({
    mutationFn: () => boardsApi.addColumn(boardId, name),
    onSuccess: () => {
      setName('');
      queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId) });
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (name.trim()) {
      add.mutate();
    }
  }

  return (
    <form className="board-column add-column" onSubmit={submit}>
      <input
        aria-label="New column name"
        placeholder="+ Add a column"
        value={name}
        maxLength={60}
        onChange={(e) => setName(e.target.value)}
      />
      {name.trim() && (
        <button type="submit" className="primary" disabled={add.isPending}>
          Add column
        </button>
      )}
      <ErrorMessage error={add.error} />
    </form>
  );
}
