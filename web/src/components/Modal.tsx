import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';

interface ModalProps {
  label: string;
  onClose: () => void;
  children: ReactNode;
  wide?: boolean;
}

export function Modal({ label, onClose, children, wide }: ModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);

  // Put focus back where it was (usually the card tile) when the dialog closes,
  // so keyboard users don't land at the top of the page.
  useEffect(() => {
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    return () => {
      if (opener?.isConnected) {
        opener.focus();
      }
    };
  }, []);

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      if (event.key !== 'Escape' || event.defaultPrevented) {
        return;
      }
      // Escape in a text field means "stop editing", not "throw my changes away".
      const target = event.target as HTMLElement | null;
      if (target?.closest('input, textarea, select')) {
        target.blur();
        return;
      }
      onClose();
    }
    document.addEventListener('keydown', onKey);
    dialogRef.current?.focus();
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  return (
    <div className="modal-backdrop" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div
        ref={dialogRef}
        className={wide ? 'modal wide' : 'modal'}
        role="dialog"
        aria-modal="true"
        aria-label={label}
        tabIndex={-1}
      >
        <button
          type="button"
          className="icon-button modal-close"
          aria-label="Close"
          onClick={onClose}
        >
          ×
        </button>
        {children}
      </div>
    </div>
  );
}
