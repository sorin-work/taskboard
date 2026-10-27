import { createEvent, fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';
import type { Column } from '../api/types';
import { BoardColumn } from './BoardColumn';

const column: Column = { id: 'todo', name: 'To do', position: 0, wipLimit: null };

function renderColumn(draggingId: string | null) {
  return (
    <QueryClientProvider client={new QueryClient()}>
      <BoardColumn
        boardId="b1"
        column={column}
        columnCount={1}
        cards={[]}
        labels={[]}
        members={[]}
        draggingId={draggingId}
        onOpenCard={() => {}}
        onDragStart={() => {}}
        onDragEnd={() => {}}
        onDropCard={() => {}}
        onKeyboardMove={() => {}}
      />
    </QueryClientProvider>
  );
}

describe('BoardColumn', () => {
  it('clears the drop highlight when a drag is cancelled', () => {
    const { rerender } = render(renderColumn('c1'));
    const section = screen.getByRole('region', { name: 'To do' });

    const dragOver = createEvent.dragOver(section);
    Object.defineProperty(dragOver, 'dataTransfer', { value: { dropEffect: 'none' } });
    fireEvent(section, dragOver);
    expect(section).toHaveClass('drop-target');
    expect(screen.getByText('Drop here')).toBeInTheDocument();

    rerender(renderColumn(null));

    expect(section).not.toHaveClass('drop-target');
    expect(screen.getByText('No cards')).toBeInTheDocument();
  });
});
