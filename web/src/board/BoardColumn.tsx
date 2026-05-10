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
}: BoardColumnProps) {
  const listRef = useRef<HTMLOListElement>(null);
  const [dropAt, setDropAt] = useState<number | null>(null);

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
