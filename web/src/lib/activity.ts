import type { Activity } from '../api/types';

/**
 * One line describing an activity entry, without the actor's name.
 * `onCard` drops the card title for feeds that are already about one card.
 */
export function describeActivity(activity: Activity, onCard = false): string {
  const d = activity.data;
  const card = onCard ? 'this card' : `"${activity.cardTitle ?? d.title ?? 'a card'}"`;
  switch (activity.type) {
    case 'board.created':
      return d.copiedFrom
        ? `created the board as a copy of "${d.copiedFrom}"`
        : 'created the board';
    case 'column.created':
      return `added the column "${d.column}"`;
    case 'column.renamed':
      return `renamed the column "${d.from}" to "${d.column}"`;
    case 'column.deleted':
      return `deleted the column "${d.column}"`;
    case 'card.created':
      if (d.copiedFrom) {
        return onCard
          ? `created this card as a copy of "${d.copiedFrom}"`
          : `copied "${d.copiedFrom}" to ${card}`;
      }
      return onCard ? `created this card in ${d.column}` : `added ${card} to ${d.column}`;
    case 'card.renamed':
      return `renamed "${d.from}" to "${d.title}"`;
    case 'card.description_changed':
      return `updated the description of ${card}`;
    case 'card.moved':
      return `moved ${card} from ${d.from} to ${d.to}`;
    case 'card.due_date_set':
      return `set ${card} to be due ${d.dueDate}`;
    case 'card.due_date_cleared':
      return `removed the due date from ${card}`;
    case 'card.completed':
      return `marked ${card} as complete`;
    case 'card.reopened':
      return `reopened ${card}`;
    case 'card.assigned':
      return `assigned ${d.user} to ${card}`;
    case 'card.unassigned':
      return `removed ${d.user} from ${card}`;
    case 'card.label_added':
      return `added the label "${d.label}" to ${card}`;
    case 'card.label_removed':
      return `removed the label "${d.label}" from ${card}`;
    case 'card.archived':
      return `archived ${card}`;
    case 'card.restored':
      return `restored ${card} from the archive`;
    case 'card.deleted':
      return `deleted ${card}`;
    case 'comment.added':
      return `commented on ${card}: “${d.excerpt}”`;
    case 'checklist.item_completed':
      return `completed "${d.item}" on ${card}`;
    default:
      return `changed ${card}`;
  }
}
