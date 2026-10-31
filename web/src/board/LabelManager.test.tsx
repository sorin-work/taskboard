import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { Board } from '../api/types';
import { mockApi, renderWithProviders } from '../test/utils';
import { LabelManager } from './LabelManager';

const board = {
  id: 'b1',
  labels: [{ id: 'l1', name: 'Bug', color: 'red' }],
} as unknown as Board;

describe('LabelManager', () => {
  afterEach(() => vi.unstubAllGlobals());

  it('creates a label with the chosen colour', async () => {
    const fetchMock = mockApi({
      'POST /api/boards/b1/labels': () => ({
        status: 201,
        body: { id: 'l2', name: 'Docs', color: 'teal' },
      }),
    });
    renderWithProviders(<LabelManager board={board} onClose={() => {}} />);

    const [, newName] = screen.getAllByLabelText('Label name');
    const [, newColour] = screen.getAllByLabelText('Label colour');
    await userEvent.type(newName!, 'Docs');
    await userEvent.selectOptions(newColour!, 'teal');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalled());
    expect(JSON.parse(fetchMock.mock.calls[0]![1]!.body as string)).toEqual({
      name: 'Docs',
      color: 'teal',
    });
  });

  it('shows the conflict when a name is taken', async () => {
    mockApi({
      'POST /api/boards/b1/labels': () => ({
        status: 409,
        body: { title: 'Conflict', detail: 'This board already has a label called "bug".' },
      }),
    });
    renderWithProviders(<LabelManager board={board} onClose={() => {}} />);

    await userEvent.type(screen.getAllByLabelText('Label name')[1]!, 'bug');
    await userEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already has a label called "bug"');
  });

  it('only offers save on an existing label once something changed', async () => {
    mockApi({});
    renderWithProviders(<LabelManager board={board} onClose={() => {}} />);

    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
    await userEvent.type(screen.getAllByLabelText('Label name')[0]!, 's');
    expect(screen.getByRole('button', { name: 'Save' })).toBeInTheDocument();
  });
});
