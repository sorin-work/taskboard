import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cardsApi } from '../api/cards';
import type { Board, Card } from '../api/types';
import { Avatar } from '../components/Avatar';
import { ErrorMessage } from '../components/ErrorMessage';
import { LabelChip } from '../components/LabelChip';
import { queryKeys } from '../lib/queryKeys';

function toggle(ids: string[], id: string): string[] {
  return ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id];
}

function useCardSetter(board: Board, card: Card, fn: (ids: string[]) => Promise<Card>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: fn,
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.card(card.id), updated);
      queryClient.invalidateQueries({ queryKey: ['boards', board.id, 'cards'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.cardActivity(card.id) });
    },
  });
}

export function AssigneePicker({ board, card }: { board: Board; card: Card }) {
  const setAssignees = useCardSetter(board, card, (ids) => cardsApi.setAssignees(card.id, ids));
  const assigned = card.assignees.map((a) => a.id);

  return (
    <fieldset className="picker">
      <legend>Assignees</legend>
      {board.members.map((member) => (
        <label key={member.userId} className="picker-option">
          <input
            type="checkbox"
            checked={assigned.includes(member.userId)}
            disabled={setAssignees.isPending}
            onChange={() => setAssignees.mutate(toggle(assigned, member.userId))}
          />
          <Avatar id={member.userId} name={member.displayName} size={22} />
          {member.displayName}
        </label>
      ))}
      <ErrorMessage error={setAssignees.error} />
    </fieldset>
  );
}

export function LabelPicker({ board, card }: { board: Board; card: Card }) {
  const setLabels = useCardSetter(board, card, (ids) => cardsApi.setLabels(card.id, ids));
  const selected = card.labels.map((l) => l.id);

  return (
    <fieldset className="picker">
      <legend>Labels</legend>
      {board.labels.length === 0 && <p className="muted small">This board has no labels yet.</p>}
      {board.labels.map((label) => (
        <label key={label.id} className="picker-option">
          <input
            type="checkbox"
            checked={selected.includes(label.id)}
            disabled={setLabels.isPending}
            onChange={() => setLabels.mutate(toggle(selected, label.id))}
          />
          <LabelChip label={label} />
        </label>
      ))}
      <ErrorMessage error={setLabels.error} />
    </fieldset>
  );
}
