import type { Activity } from '../api/types';
import { Avatar } from '../components/Avatar';
import { describeActivity } from '../lib/activity';
import { formatRelative } from '../lib/dates';

interface ActivityListProps {
  items: Activity[];
  onCard?: boolean;
  onOpenCard?: (cardId: string) => void;
}

export function ActivityList({ items, onCard = false, onOpenCard }: ActivityListProps) {
  if (items.length === 0) {
    return <p className="muted small">Nothing has happened yet.</p>;
  }
  return (
    <ol className="activity-list">
      {items.map((item) => (
        <li key={item.id}>
          <Avatar id={item.actor.id} name={item.actor.displayName} size={22} />
          <div>
            <p>
              <strong>{item.actor.displayName}</strong> {describeActivity(item, onCard)}
            </p>
            <span className="muted small">
              {formatRelative(item.createdAt)}
              {!onCard && item.cardId && onOpenCard && (
                <>
                  {' · '}
                  <button type="button" className="link" onClick={() => onOpenCard(item.cardId!)}>
                    open card
                  </button>
                </>
              )}
            </span>
          </div>
        </li>
      ))}
    </ol>
  );
}
