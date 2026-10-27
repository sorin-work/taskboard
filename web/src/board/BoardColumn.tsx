import { useRef, useState } from 'react';
import type { DragEvent } from 'react';
import type { BoardMember, CardSummary, Column, Label } from '../api/types';
import type { Direction } from '../lib/keyboardMove';
import { afterCardFor, dropIndex } from '../lib/ordering';
import { AddCardForm } from './AddCardForm';
import { CardTile } from './CardTile';
import { ColumnHeader } from './ColumnHeader';

interface BoardColumnProps {
  boardId: string;
  column: Column;
  columnCount: number;
  cards: CardSummary[];
  labels: Label[];
  members: BoardMember[];
  draggingId: string | null;
  onOpenCard: (cardId: string) => void;
  onDragStart: (cardId: string) => void;
  onDragEnd: () => void;
  onDropCard: (cardId: string, columnId: string, afterCardId: string | null) => void;
  onKeyboardMove: (cardId: string, direction: Direction) => void;
  collapsed?: boolean;
  onToggleCollapsed?: () => void;
}

export function BoardColumn({
  boardId,
  column,
  columnCount,
  cards,
  labels,
  members,
  draggingId,
  onOpenCard,
  onDragStart,
  onDragEnd,
  onDropCard,
  onKeyboardMove,
  collapsed = false,
  onToggleCollapsed,
}: BoardColumnProps) {
  const listRef = useRef<HTMLOListElement>(null);
  const [hoverAt, setDropAt] = useState<number | null>(null);
  // A drag cancelled with Escape (or dropped outside the board) never fires
  // dragleave on this column, so only show the indicator while a drag is on.
  const dropAt = draggingId ? hoverAt : null;

  function indexFor(event: DragEvent) {
    const tiles = Array.from(
      listRef.current?.querySelectorAll<HTMLElement>('[data-card-id]') ?? [],
    );
    return dropIndex(
      event.clientY,
      tiles.map((tile) => tile.getBoundingClientRect()),
    );
  }

  function handleDragOver(event: DragEvent) {
    if (!draggingId) {
      return;
    }
    event.preventDefault();
    event.dataTransfer.dropEffect = 'move';
    setDropAt(indexFor(event));
  }

  function handleDrop(event: DragEvent) {
    event.preventDefault();
    const cardId = event.dataTransfer.getData('text/plain') || draggingId;
    setDropAt(null);
    if (cardId) {
      onDropCard(cardId, column.id, afterCardFor(cards, cardId, indexFor(event)));
    }
  }

  if (collapsed) {
    return (
      <section className="board-column collapsed" aria-label={column.name}>
        <button
          type="button"
          className="collapsed-toggle"
          aria-label={`Expand ${column.name}`}
          onClick={onToggleCollapsed}
        >
          <span className="collapsed-name">{column.name}</span>
          <span className="count">{cards.length}</span>
        </button>
      </section>
    );
  }

  return (
    <section
      className={[
        'board-column',
        dropAt !== null && 'drop-target',
        column.wipLimit !== null && cards.length > column.wipLimit && 'over-limit',
      ]
        .filter(Boolean)
        .join(' ')}
      aria-label={column.name}
      onDragOver={handleDragOver}
      onDragLeave={(e) => {
        if (!e.currentTarget.contains(e.relatedTarget as Node | null)) {
          setDropAt(null);
        }
      }}
      onDrop={handleDrop}
    >
      <ColumnHeader
        boardId={boardId}
        column={column}
        columnCount={columnCount}
        cardCount={cards.length}
        onCollapse={onToggleCollapsed}
      />
      <ol className="card-list" ref={listRef}>
        {cards.map((card, i) => (
          <li key={card.id} data-card-id={card.id}>
            {dropAt === i && <div className="drop-indicator" />}
            <CardTile
              card={card}
              labels={labels}
              members={members}
              dragging={draggingId === card.id}
              onOpen={onOpenCard}
              onDragStart={onDragStart}
              onDragEnd={onDragEnd}
              onKeyboardMove={onKeyboardMove}
            />
          </li>
        ))}
        {cards.length === 0 && (
          <li className={dropAt !== null ? 'empty-column active' : 'empty-column'}>
            {draggingId ? 'Drop here' : 'No cards'}
          </li>
        )}
        {cards.length > 0 && dropAt === cards.length && (
          <li className="drop-indicator" aria-hidden />
        )}
      </ol>
      <AddCardForm boardId={boardId} columnId={column.id} />
    </section>
  );
}
