import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { cardsApi } from '../api/cards';
import type { Card, Checklist } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { queryKeys } from '../lib/queryKeys';

function useRefreshCard(card: Card) {
  const queryClient = useQueryClient();
  return () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.card(card.id) });
    queryClient.invalidateQueries({ queryKey: ['boards', card.boardId, 'cards'] });
  };
}

export function ChecklistSection({ card }: { card: Card }) {
  const refresh = useRefreshCard(card);
  const [title, setTitle] = useState('');
  const [adding, setAdding] = useState(false);
  const add = useMutation({
    mutationFn: () => cardsApi.addChecklist(card.id, title.trim() || 'Checklist'),
    onSuccess: () => {
      setTitle('');
      setAdding(false);
      refresh();
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    add.mutate();
  }

  return (
    <section>
      {card.checklists.map((checklist) => (
        <ChecklistView key={checklist.id} card={card} checklist={checklist} />
      ))}
      {adding ? (
        <form className="inline-add" onSubmit={submit}>
          <input
            aria-label="Checklist title"
            autoFocus
            placeholder="Checklist"
            value={title}
            maxLength={100}
            onChange={(e) => setTitle(e.target.value)}
          />
          <button className="primary" disabled={add.isPending}>
            Add
          </button>
          <button type="button" onClick={() => setAdding(false)}>
            Cancel
          </button>
        </form>
      ) : (
        <button type="button" onClick={() => setAdding(true)}>
          + Add a checklist
        </button>
      )}
      <ErrorMessage error={add.error} />
    </section>
  );
}

export function ChecklistView({ card, checklist }: { card: Card; checklist: Checklist }) {
  const refresh = useRefreshCard(card);
  const [text, setText] = useState('');
  const done = checklist.items.filter((i) => i.done).length;
  const total = checklist.items.length;
  const percent = total === 0 ? 0 : Math.round((done / total) * 100);

  const addItem = useMutation({
    mutationFn: () => cardsApi.addItem(checklist.id, text.trim()),
    onSuccess: () => {
      setText('');
      refresh();
    },
  });
  const toggle = useMutation({
    mutationFn: ({ id, itemText, value }: { id: string; itemText: string; value: boolean }) =>
      cardsApi.updateItem(id, itemText, value),
    onSuccess: refresh,
  });
  const moveItem = useMutation({
    mutationFn: ({ id, position }: { id: string; position: number }) =>
      cardsApi.moveItem(id, position),
    onSuccess: refresh,
  });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editText, setEditText] = useState('');
  const removeItem = useMutation({
    mutationFn: (id: string) => cardsApi.deleteItem(id),
    onSuccess: refresh,
  });
  const removeChecklist = useMutation({
    mutationFn: () => cardsApi.deleteChecklist(checklist.id),
    onSuccess: refresh,
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (text.trim()) {
      addItem.mutate();
    }
  }

  return (
    <div className="checklist">
      <div className="checklist-header">
        <h3>{checklist.title}</h3>
        <span className="muted small">
          {done}/{total}
        </span>
        <button
          type="button"
          className="icon-button"
          aria-label={`Delete checklist ${checklist.title}`}
          onClick={() =>
            window.confirm(`Delete the "${checklist.title}" checklist?`) && removeChecklist.mutate()
          }
        >
          ×
        </button>
      </div>
      <div
        className="progress"
        role="progressbar"
        aria-valuenow={percent}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`${checklist.title} progress`}
      >
        <div style={{ width: `${percent}%` }} />
      </div>
      <ul className="checklist-items">
        {checklist.items.map((item, index) => (
          <li key={item.id} className={item.done ? 'done' : undefined}>
            {editingId === item.id ? (
              <form
                className="inline-add"
                onSubmit={(e) => {
                  e.preventDefault();
                  if (editText.trim()) {
                    toggle.mutate({ id: item.id, itemText: editText.trim(), value: item.done });
                  }
                  setEditingId(null);
                }}
              >
                <input
                  aria-label={`Edit ${item.text}`}
                  autoFocus
                  value={editText}
                  maxLength={300}
                  onChange={(e) => setEditText(e.target.value)}
                  onBlur={() => setEditingId(null)}
                />
              </form>
            ) : (
              <label>
                <input
                  type="checkbox"
                  checked={item.done}
                  onChange={(e) =>
                    toggle.mutate({ id: item.id, itemText: item.text, value: e.target.checked })
                  }
                />
                {item.text}
              </label>
            )}
            <span className="item-actions">
              <button
                type="button"
                className="icon-button"
                aria-label={`Rename ${item.text}`}
                onClick={() => {
                  setEditText(item.text);
                  setEditingId(item.id);
                }}
              >
                ✎
              </button>
              <button
                type="button"
                className="icon-button"
                aria-label={`Move ${item.text} up`}
                disabled={index === 0 || moveItem.isPending}
                onClick={() => moveItem.mutate({ id: item.id, position: index - 1 })}
              >
                ↑
              </button>
              <button
                type="button"
                className="icon-button"
                aria-label={`Move ${item.text} down`}
                disabled={index === checklist.items.length - 1 || moveItem.isPending}
                onClick={() => moveItem.mutate({ id: item.id, position: index + 1 })}
              >
                ↓
              </button>
              <button
                type="button"
                className="icon-button"
                aria-label={`Delete ${item.text}`}
                onClick={() => removeItem.mutate(item.id)}
              >
                ×
              </button>
            </span>
          </li>
        ))}
      </ul>
      <form className="inline-add" onSubmit={submit}>
        <input
          aria-label={`Add an item to ${checklist.title}`}
          placeholder="Add an item"
          value={text}
          maxLength={300}
          onChange={(e) => setText(e.target.value)}
        />
        {text.trim() && (
          <button className="primary" disabled={addItem.isPending}>
            Add
          </button>
        )}
      </form>
      <ErrorMessage
        error={
          addItem.error ??
          toggle.error ??
          moveItem.error ??
          removeItem.error ??
          removeChecklist.error
        }
      />
    </div>
  );
}
