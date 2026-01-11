import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import type { Board, Label, LabelColor } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { LabelChip } from '../components/LabelChip';
import { Modal } from '../components/Modal';
import { queryKeys } from '../lib/queryKeys';

export const LABEL_COLORS: LabelColor[] = [
  'gray',
  'red',
  'orange',
  'yellow',
  'green',
  'teal',
  'blue',
  'purple',
  'pink',
];

export function LabelManager({ board, onClose }: { board: Board; onClose: () => void }) {
  return (
    <Modal label="Board labels" onClose={onClose}>
      <h2>Labels</h2>
      <ul className="label-list">
        {board.labels.map((label) => (
          <li key={label.id}>
            <LabelEditor board={board} label={label} />
          </li>
        ))}
      </ul>
      <h3>New label</h3>
      <LabelEditor board={board} />
    </Modal>
  );
}

function LabelEditor({ board, label }: { board: Board; label?: Label }) {
  const queryClient = useQueryClient();
  const [name, setName] = useState(label?.name ?? '');
  const [color, setColor] = useState<LabelColor>(label?.color ?? 'blue');
  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.board(board.id) });
    queryClient.invalidateQueries({ queryKey: ['cards'] });
  };

  const save = useMutation({
    mutationFn: () =>
      label
        ? boardsApi.updateLabel(label.id, name, color)
        : boardsApi.createLabel(board.id, name, color),
    onSuccess: () => {
      if (!label) {
        setName('');
      }
      refresh();
    },
  });
  const remove = useMutation({
    mutationFn: () => boardsApi.deleteLabel(label!.id),
    onSuccess: refresh,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    save.mutate();
  }

  const dirty = !label || name !== label.name || color !== label.color;

  return (
    <form className="label-editor" onSubmit={submit}>
      <LabelChip label={{ id: label?.id ?? 'new', name: name || 'Label', color }} />
      <input
        aria-label="Label name"
        value={name}
        maxLength={40}
        placeholder="Name"
        onChange={(e) => setName(e.target.value)}
        required
      />
      <select
        aria-label="Label colour"
        value={color}
        onChange={(e) => setColor(e.target.value as LabelColor)}
      >
        {LABEL_COLORS.map((c) => (
          <option key={c} value={c}>
            {c}
          </option>
        ))}
      </select>
      {dirty && (
        <button type="submit" className="primary" disabled={save.isPending}>
          {label ? 'Save' : 'Add'}
        </button>
      )}
      {label && (
        <button
          type="button"
          className="icon-button danger"
          aria-label={`Delete label ${label.name}`}
          onClick={() =>
            window.confirm(
              `Delete the "${label.name}" label? It will be removed from all cards.`,
            ) && remove.mutate()
          }
        >
          ×
        </button>
      )}
      <ErrorMessage error={save.error ?? remove.error} />
    </form>
  );
}
