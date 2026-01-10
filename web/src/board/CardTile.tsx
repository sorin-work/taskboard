import type { DragEvent } from 'react';
import type { CardSummary } from '../api/types';

interface CardTileProps {
  card: CardSummary;
  dragging?: boolean;
  onOpen: (cardId: string) => void;
  onDragStart?: (cardId: string) => void;
  onDragEnd?: () => void;
}

export function CardTile({ card, dragging, onOpen, onDragStart, onDragEnd }: CardTileProps) {
  function handleDragStart(event: DragEvent) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', card.id);
    onDragStart?.(card.id);
  }

  return (
    <button
      type="button"
      className={dragging ? 'card-tile dragging' : 'card-tile'}
      draggable
      onDragStart={handleDragStart}
      onDragEnd={onDragEnd}
      onClick={() => onOpen(card.id)}
    >
      <span className="card-title">{card.title}</span>
    </button>
  );
}
