import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import type { Column } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

interface ColumnHeaderProps {
  boardId: string;
  column: Column;
  columnCount: number;
  cardCount: number;
}

export function ColumnHeader({ boardId, column, columnCount, cardCount }: ColumnHeaderProps) {
  const queryClient = useQueryClient();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(column.name);
  const [menuOpen, setMenuOpen] = useState(false);

  const refresh = () => queryClient.invalidateQueries({ queryKey: queryKeys.board(boardId) });
  const rename = useMutation({
    mutationFn: () => boardsApi.renameColumn(column.id, name),
    onSuccess: () => {
      setEditing(false);
      refresh();
    },
  });
  const move = useMutation({
    mutationFn: (position: number) => boardsApi.moveColumn(column.id, position),
    onSuccess: refresh,
  });
  const remove = useMutation({
    mutationFn: () => boardsApi.deleteColumn(column.id),
    onSuccess: () => {
      refresh();
      queryClient.invalidateQueries({ queryKey: ['boards', boardId, 'cards'] });
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (name.trim() && name.trim() !== column.name) {
      rename.mutate();
    } else {
      setEditing(false);
    }
  }

  function confirmDelete() {
    setMenuOpen(false);
    const message =
      cardCount > 0
        ? `Delete "${column.name}" and its ${cardCount} cards?`
        : `Delete "${column.name}"?`;
    if (window.confirm(message)) {
      remove.mutate();
    }
  }

  return (
    <header className="column-header">
      {editing ? (
        <form onSubmit={submit} className="column-rename">
          <input
            aria-label="Column name"
            autoFocus
            value={name}
            maxLength={60}
            onChange={(e) => setName(e.target.value)}
            onBlur={submit}
            onKeyDown={(e) => e.key === 'Escape' && setEditing(false)}
          />
        </form>
      ) : (
        <h2 onDoubleClick={() => setEditing(true)}>{column.name}</h2>
      )}
      <span className="count">{cardCount}</span>
      <div className="menu">
        <button
          type="button"
          className="icon-button"
          aria-label={`Column actions for ${column.name}`}
          aria-expanded={menuOpen}
          onClick={() => setMenuOpen((open) => !open)}
        >
          ⋯
        </button>
        {menuOpen && (
          <div className="menu-popover" role="menu" onMouseLeave={() => setMenuOpen(false)}>
            <button
              role="menuitem"
              onClick={() => {
                setMenuOpen(false);
                setName(column.name);
                setEditing(true);
              }}
            >
              Rename
            </button>
            <button
              role="menuitem"
              disabled={column.position === 0}
              onClick={() => {
                setMenuOpen(false);
                move.mutate(column.position - 1);
              }}
            >
              Move left
            </button>
            <button
              role="menuitem"
              disabled={column.position === columnCount - 1}
              onClick={() => {
                setMenuOpen(false);
                move.mutate(column.position + 1);
              }}
            >
              Move right
            </button>
            <button role="menuitem" className="danger" onClick={confirmDelete}>
              Delete column
            </button>
          </div>
        )}
      </div>
      <ErrorMessage error={rename.error ?? move.error ?? remove.error} />
    </header>
  );
}
