import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { BoardMember, CardSummary, Label } from '../api/types';
import { addDays, todayIso } from '../lib/dates';
import { CardTile } from './CardTile';

const labels: Label[] = [
  { id: 'l1', name: 'Bug', color: 'red' },
  { id: 'l2', name: 'Design', color: 'purple' },
];
const members: BoardMember[] = [
  { userId: 'u1', displayName: 'Dana Scully', email: 'dana@example.com' },
  { userId: 'u2', displayName: 'Fox Mulder', email: 'fox@example.com' },
];

function card(overrides: Partial<CardSummary> = {}): CardSummary {
  return {
    id: 'c1',
    columnId: 'col',
    title: 'Fix the login page',
    position: 1024,
    dueDate: null,
    completed: false,
    assigneeIds: [],
    labelIds: [],
    checklistDone: 0,
    checklistTotal: 0,
    commentCount: 0,
    ...overrides,
  };
}

describe('CardTile', () => {
  it('shows labels, assignees and counts', () => {
    render(
      <CardTile
        card={card({
          labelIds: ['l1'],
          assigneeIds: ['u2'],
          checklistDone: 1,
          checklistTotal: 3,
          commentCount: 2,
        })}
        labels={labels}
        members={members}
        onOpen={() => {}}
      />,
    );

    expect(screen.getByTitle('Bug')).toBeInTheDocument();
    expect(screen.queryByTitle('Design')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Fox Mulder')).toHaveTextContent('FM');
    expect(screen.getByTitle('Checklist items done')).toHaveTextContent('1/3');
    expect(screen.getByTitle('Comments')).toHaveTextContent('2');
  });

  it('marks overdue cards', () => {
    render(
      <CardTile
        card={card({ dueDate: addDays(todayIso(), -1) })}
        labels={labels}
        members={members}
        onOpen={() => {}}
      />,
    );

    expect(screen.getByText('Yesterday')).toHaveClass('due-overdue');
  });

  it('opens the card on click', async () => {
    const onOpen = vi.fn();
    render(<CardTile card={card()} labels={[]} members={[]} onOpen={onOpen} />);

    await userEvent.click(screen.getByRole('button', { name: /Fix the login page/ }));

    expect(onOpen).toHaveBeenCalledWith('c1');
  });
});
