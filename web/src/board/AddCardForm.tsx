import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cardsApi } from '../api/cards';
import { ErrorMessage } from '../components/ErrorMessage';

export function AddCardForm({ boardId, columnId }: { boardId: string; columnId: string }) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState('');
  const create = useMutation({
    mutationFn: () => cardsApi.create(columnId, title),
    onSuccess: () => {
      setTitle('');
      queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'cards'] });
    },
  });

  if (!open) {
    return (
      <button type="button" className="add-card-button" onClick={() => setOpen(true)}>
        + Add a card
      </button>
    );
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    if (title.trim()) {
      create.mutate();
    }
  }

  return (
    <form className="add-card-form" onSubmit={submit}>
      <textarea
        aria-label="Card title"
        placeholder="What needs doing?"
        autoFocus
        value={title}
        maxLength={200}
        onChange={(e) => setTitle(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            submit(e);
          } else if (e.key === 'Escape') {
            setOpen(false);
          }
        }}
      />
      <div className="row">
        <button type="submit" className="primary" disabled={create.isPending}>
          Add card
        </button>
        <button type="button" onClick={() => setOpen(false)}>
          Cancel
        </button>
      </div>
      <ErrorMessage error={create.error} />
    </form>
  );
}
