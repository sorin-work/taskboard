import type { CardSummary } from '../api/types';

interface CardTileProps {
  card: CardSummary;
  onOpen: (cardId: string) => void;
}

export function CardTile({ card, onOpen }: CardTileProps) {
  return (
    <button type="button" className="card-tile" onClick={() => onOpen(card.id)}>
      <span className="card-title">{card.title}</span>
    </button>
  );
}
