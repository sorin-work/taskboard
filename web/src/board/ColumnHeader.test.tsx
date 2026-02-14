import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, expect, it } from 'vitest';
import type { Column } from '../api/types';
import { ColumnHeader } from './ColumnHeader';

function renderHeader(column: Column, cardCount: number) {
  return render(
    <QueryClientProvider client={new QueryClient()}>
      <ColumnHeader boardId="b1" column={column} columnCount={3} cardCount={cardCount} />
    </QueryClientProvider>,
  );
}

describe('ColumnHeader', () => {
  it('shows a plain count without a limit', () => {
    renderHeader({ id: 'c', name: 'Doing', position: 1, wipLimit: null }, 4);
    expect(screen.getByText('4')).not.toHaveClass('over-limit');
  });

  it('shows the count against the limit', () => {
    renderHeader({ id: 'c', name: 'Doing', position: 1, wipLimit: 3 }, 2);
    expect(screen.getByText('2 / 3')).not.toHaveClass('over-limit');
  });

  it('flags a column over its limit', () => {
    renderHeader({ id: 'c', name: 'Doing', position: 1, wipLimit: 3 }, 5);
    expect(screen.getByText('5 / 3')).toHaveClass('over-limit');
    expect(screen.getByTitle('Over the limit of 3')).toBeInTheDocument();
  });
});
