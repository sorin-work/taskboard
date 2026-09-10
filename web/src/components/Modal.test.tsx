import { useState } from 'react';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { Modal } from './Modal';

function renderModal(onClose = vi.fn()) {
  render(
    <Modal label="Card" onClose={onClose}>
      <textarea aria-label="Description" />
    </Modal>,
  );
  return onClose;
}

describe('Modal', () => {
  it('closes on Escape', async () => {
    const onClose = renderModal();
    await userEvent.keyboard('{Escape}');
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('keeps the dialog open when Escape is pressed in a text field', async () => {
    const onClose = renderModal();
    const field = screen.getByLabelText('Description');

    await userEvent.type(field, 'half-written thought');
    await userEvent.keyboard('{Escape}');

    expect(onClose).not.toHaveBeenCalled();
    expect(field).not.toHaveFocus();
  });

  it('closes from the close button and the backdrop', async () => {
    const onClose = renderModal();

    await userEvent.click(screen.getByRole('button', { name: 'Close' }));
    expect(onClose).toHaveBeenCalledTimes(1);

    await userEvent.click(screen.getByRole('dialog').parentElement!);
    expect(onClose).toHaveBeenCalledTimes(2);
  });

  it('gives focus back to whatever opened it', async () => {
    function Harness() {
      const [open, setOpen] = useState(false);
      return (
        <>
          <button onClick={() => setOpen(true)}>Open card</button>
          {open && (
            <Modal label="Card" onClose={() => setOpen(false)}>
              <p>details</p>
            </Modal>
          )}
        </>
      );
    }
    render(<Harness />);

    await userEvent.click(screen.getByRole('button', { name: 'Open card' }));
    expect(screen.getByRole('dialog')).toHaveFocus();
    await userEvent.keyboard('{Escape}');

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open card' })).toHaveFocus();
  });
});
