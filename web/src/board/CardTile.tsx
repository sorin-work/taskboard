import type { DragEvent } from 'react';
import type { BoardMember, CardSummary, Label } from '../api/types';
import { Avatar } from '../components/Avatar';
import { LabelChip } from '../components/LabelChip';
import { dueStatus, formatDue, todayIso } from '../lib/dates';

interface CardTileProps {
  card: CardSummary;
  labels: Label[];
  members: BoardMember[];
  dragging?: boolean;
  onOpen: (cardId: string) => void;
  onDragStart?: (cardId: string) => void;
  onDragEnd?: () => void;
}

export function CardTile({
  card,
  labels,
  members,
  dragging,
  onOpen,
  onDragStart,
  onDragEnd,
}: CardTileProps) {
  const today = todayIso();
  const status = dueStatus(card.dueDate, card.completed, today);
  const cardLabels = labels.filter((l) => card.labelIds.includes(l.id));
  const assignees = members.filter((m) => card.assigneeIds.includes(m.userId));

  function handleDragStart(event: DragEvent) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', card.id);
    onDragStart?.(card.id);
  }

  const classes = ['card-tile'];
  if (dragging) classes.push('dragging');
  if (card.completed) classes.push('completed');

  return (
    <button
      type="button"
      className={classes.join(' ')}
      draggable
      onDragStart={handleDragStart}
      onDragEnd={onDragEnd}
      onClick={() => onOpen(card.id)}
    >
      {cardLabels.length > 0 && (
        <span className="label-row">
          {cardLabels.map((label) => (
            <LabelChip key={label.id} label={label} compact />
          ))}
        </span>
      )}
      <span className="card-title">{card.title}</span>
      <span className="card-meta">
        {card.dueDate && status && (
          <span className={`due-badge due-${status}`}>{formatDue(card.dueDate, today)}</span>
        )}
        {card.checklistTotal > 0 && (
          <span
            className={card.checklistDone === card.checklistTotal ? 'meta done' : 'meta'}
            title="Checklist items done"
          >
            ☑ {card.checklistDone}/{card.checklistTotal}
          </span>
        )}
        {card.commentCount > 0 && (
          <span className="meta" title="Comments">
            💬 {card.commentCount}
          </span>
        )}
        {assignees.length > 0 && (
          <span className="assignees">
            {assignees.map((m) => (
              <Avatar key={m.userId} id={m.userId} name={m.displayName} size={22} />
            ))}
          </span>
        )}
      </span>
    </button>
  );
}
