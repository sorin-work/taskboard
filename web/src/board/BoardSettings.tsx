import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import type { Board } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { Modal } from '../components/Modal';
import { queryKeys } from '../lib/queryKeys';

export function BoardSettings({ board, onClose }: { board: Board; onClose: () => void }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [name, setName] = useState(board.name);
  const [description, setDescription] = useState(board.description);
  const canAdminister = board.role !== 'member';

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.board(board.id) });
    queryClient.invalidateQueries({ queryKey: ['workspaces', board.workspaceId, 'boards'] });
  };
  const save = useMutation({
    mutationFn: (archived: boolean) => boardsApi.update(board.id, { name, description, archived }),
    onSuccess: () => {
      refresh();
      onClose();
    },
  });
  const remove = useMutation({
    mutationFn: () => boardsApi.remove(board.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workspaces', board.workspaceId, 'boards'] });
      navigate(`/w/${board.workspaceId}`, { replace: true });
    },
  });

  const archived = board.archivedAt !== null;

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate(archived);
  }

  return (
    <Modal label="Board settings" onClose={onClose}>
      <form className="settings-form" onSubmit={submit}>
        <h2>Board settings</h2>
        <div className="field">
          <label htmlFor="board-name">Name</label>
          <input
            id="board-name"
            value={name}
            maxLength={100}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>
        <div className="field">
          <label htmlFor="board-description">Description</label>
          <textarea
            id="board-description"
            rows={3}
            maxLength={2000}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>
        <ErrorMessage error={save.error ?? remove.error} />
        <div className="row">
          <button className="primary" disabled={save.isPending}>
            Save
          </button>
          <button type="button" onClick={onClose}>
            Cancel
          </button>
        </div>
      </form>

      {canAdminister && (
        <section className="danger-zone">
          <h3>Archive or delete</h3>
          <p className="muted small">
            Archived boards are hidden from the workspace and from My tasks, but nothing is lost.
          </p>
          <div className="row">
            <button type="button" onClick={() => save.mutate(!archived)} disabled={save.isPending}>
              {archived ? 'Restore board' : 'Archive board'}
            </button>
            <button
              type="button"
              className="danger"
              disabled={remove.isPending}
              onClick={() =>
                window.confirm(
                  `Delete "${board.name}" and all of its cards? This can't be undone.`,
                ) && remove.mutate()
              }
            >
              Delete board
            </button>
          </div>
        </section>
      )}
    </Modal>
  );
}
