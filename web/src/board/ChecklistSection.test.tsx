import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Card, Checklist } from '../api/types';
import { mockApi } from '../test/utils';
import { ChecklistView } from './ChecklistSection';

const checklist: Checklist = {
  id: 'cl1',
  title: 'Launch steps',
  items: [
    { id: 'i1', text: 'Write post', done: true, position: 0 },
    { id: 'i2', text: 'Send newsletter', done: false, position: 1 },
  ],
};

const card = { id: 'c1', boardId: 'b1', checklists: [checklist] } as unknown as Card;

function renderChecklist() {
  const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <ChecklistView card={card} checklist={checklist} />
    </QueryClientProvider>,
  );
}

describe('ChecklistView', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('shows progress', () => {
    mockApi({});
    renderChecklist();

    expect(screen.getByText('1/2')).toBeInTheDocument();
    expect(screen.getByRole('progressbar', { name: 'Launch steps progress' })).toHaveAttribute(
      'aria-valuenow',
      '50',
    );
  });

  it('ticks an item', async () => {
    const fetchMock = mockApi({
      'PATCH /api/checklist-items/i2': () => ({
        body: { id: 'i2', text: 'Send newsletter', done: true, position: 1 },
      }),
    });
    renderChecklist();

    await userEvent.click(screen.getByLabelText('Send newsletter'));

    expect(fetchMock).toHaveBeenCalledOnce();
    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({
      text: 'Send newsletter',
      done: true,
    });
  });

  it('adds an item', async () => {
    const fetchMock = mockApi({
      'POST /api/checklists/cl1/items': () => ({
        status: 201,
        body: { id: 'i3', text: 'Tweet', done: false, position: 2 },
      }),
    });
    renderChecklist();

    await userEvent.type(screen.getByLabelText('Add an item to Launch steps'), 'Tweet{Enter}');

    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({ text: 'Tweet' });
    expect(screen.getByLabelText('Add an item to Launch steps')).toHaveValue('');
  });

  it('moves an item down', async () => {
    const fetchMock = mockApi({
      'POST /api/checklist-items/i1/move': () => ({ body: checklist }),
    });
    renderChecklist();

    expect(screen.getByLabelText('Move Write post up')).toBeDisabled();
    await userEvent.click(screen.getByLabelText('Move Write post down'));

    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({ position: 1 });
  });

  it('renames an item', async () => {
    const fetchMock = mockApi({
      'PATCH /api/checklist-items/i2': () => ({ body: {} }),
    });
    renderChecklist();

    await userEvent.click(screen.getByLabelText('Rename Send newsletter'));
    const field = screen.getByLabelText('Edit Send newsletter');
    await userEvent.clear(field);
    await userEvent.type(field, 'Send the newsletter{Enter}');

    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({
      text: 'Send the newsletter',
      done: false,
    });
  });
});
