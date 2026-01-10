import type { CardSummary, Column } from '../api/types';
import { AddCardForm } from './AddCardForm';
import { CardTile } from './CardTile';

interface BoardColumnProps {
  boardId: string;
  column: Column;
  cards: CardSummary[];
  onOpenCard: (cardId: string) => void;
}

export function BoardColumn({ boardId, column, cards, onOpenCard }: BoardColumnProps) {
  return (
    <section className="board-column" aria-label={column.name}>
      <header className="column-header">
        <h2>{column.name}</h2>
        <span className="count">{cards.length}</span>
      </header>
      <ol className="card-list">
        {cards.map((card) => (
          <li key={card.id}>
            <CardTile card={card} onOpen={onOpenCard} />
          </li>
        ))}
      </ol>
      <AddCardForm boardId={boardId} columnId={column.id} />
    </section>
  );
}
