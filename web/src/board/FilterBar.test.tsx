import { act, fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { Board } from '../api/types';
import { FilterBar } from './FilterBar';

const board = {
  id: 'b1',
  members: [{ userId: 'u1', displayName: 'Dana', email: 'd@example.com' }],
  labels: [{ id: 'l1', name: 'Bug', color: 'red' }],
} as unknown as Board;

describe('FilterBar', () => {
  beforeEach(() => vi.useFakeTimers({ shouldAdvanceTime: true }));
  afterEach(() => vi.useRealTimers());

  it('changes a select straight away', async () => {
    const onChange = vi.fn();
    render(<FilterBar board={board} filters={{ due: 'today' }} onChange={onChange} />);

    await userEvent.selectOptions(screen.getByLabelText('Label'), 'l1');

    expect(onChange).toHaveBeenCalledWith({ due: 'today', label: 'l1' });
  });

  it('waits for a pause in typing before searching', () => {
    const onChange = vi.fn();
    render(<FilterBar board={board} filters={{}} onChange={onChange} />);

    fireEvent.change(screen.getByLabelText('Search cards'), { target: { value: 'log' } });
    act(() => vi.advanceTimersByTime(100));
    fireEvent.change(screen.getByLabelText('Search cards'), { target: { value: 'login' } });
    expect(onChange).not.toHaveBeenCalled();

    act(() => vi.advanceTimersByTime(300));
    expect(onChange).toHaveBeenCalledOnce();
    expect(onChange).toHaveBeenCalledWith({ q: 'login' });
  });

  it('clears everything', async () => {
    const onChange = vi.fn();
    render(<FilterBar board={board} filters={{ assignee: 'me', q: 'x' }} onChange={onChange} />);

    await userEvent.click(screen.getByRole('button', { name: 'Clear filters' }));

    expect(onChange).toHaveBeenLastCalledWith({});
    expect(screen.getByLabelText('Search cards')).toHaveValue('');
  });

  it('hides the clear button without filters', () => {
    render(<FilterBar board={board} filters={{}} onChange={() => {}} />);
    expect(screen.queryByRole('button', { name: 'Clear filters' })).not.toBeInTheDocument();
  });
});
