import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cardsApi } from '../api/cards';
import type { CardUpdate } from '../api/cards';
import type { Board, Card } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { LabelChip } from '../components/LabelChip';
import { Modal } from '../components/Modal';
import { queryKeys } from '../lib/queryKeys';
import { AssigneePicker, LabelPicker } from './CardPeople';
import { ChecklistSection } from './ChecklistSection';
import { CommentsSection } from './CommentsSection';

interface CardModalProps {
  board: Board;
  cardId: string;
  onClose: () => void;
}

export function CardModal({ board, cardId, onClose }: CardModalProps) {
  const card = useQuery({ queryKey: queryKeys.card(cardId), queryFn: () => cardsApi.get(cardId) });

  return (
    <Modal label={card.data?.title ?? 'Card'} onClose={onClose} wide>
      {card.isPending && <p className="muted">Loading…</p>}
      <ErrorMessage error={card.error} />
      {card.data && <CardDetails board={board} card={card.data} onClose={onClose} />}
    </Modal>
  );
}

function CardDetails({ board, card, onClose }: { board: Board; card: Card; onClose: () => void }) {
  const queryClient = useQueryClient();
  const [title, setTitle] = useState(card.title);
  const [description, setDescription] = useState(card.description);
  const [editingDescription, setEditingDescription] = useState(false);

  const refreshBoard = () => {
    queryClient.invalidateQueries({ queryKey: ['boards', board.id, 'cards'] });
    queryClient.invalidateQueries({ queryKey: queryKeys.cardActivity(card.id) });
  };

  const update = useMutation({
    mutationFn: (changes: Partial<CardUpdate>) =>
      cardsApi.update(card.id, {
        title: card.title,
        description: card.description,
        dueDate: card.dueDate,
        completed: card.completedAt !== null,
        ...changes,
      }),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.card(card.id), updated);
      refreshBoard();
    },
  });

  const move = useMutation({
    mutationFn: (columnId: string) => cardsApi.move(card.id, columnId, null),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.card(card.id), updated);
      refreshBoard();
    },
  });

  const remove = useMutation({
    mutationFn: () => cardsApi.remove(card.id),
    onSuccess: () => {
      refreshBoard();
      onClose();
    },
  });

  function saveTitle() {
    const trimmed = title.trim();
    if (!trimmed) {
      setTitle(card.title);
    } else if (trimmed !== card.title) {
      update.mutate({ title: trimmed });
    }
  }

  return (
    <div className="card-details">
      <div className="card-main">
        <textarea
          className="card-title-input"
          aria-label="Title"
          value={title}
          maxLength={200}
          rows={1}
          onChange={(e) => setTitle(e.target.value)}
          onBlur={saveTitle}
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault();
              e.currentTarget.blur();
            }
          }}
        />
        <p className="muted">
          in <strong>{card.columnName}</strong> · added by {card.createdBy.displayName}
        </p>
        {card.labels.length > 0 && (
          <div className="label-row">
            {card.labels.map((label) => (
              <LabelChip key={label.id} label={label} />
            ))}
          </div>
        )}

        <section>
          <h3>Description</h3>
          {editingDescription ? (
            <div className="description-editor">
              <textarea
                aria-label="Description"
                autoFocus
                rows={6}
                maxLength={10000}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
              <div className="row">
                <button
                  className="primary"
                  onClick={() => {
                    update.mutate({ description });
                    setEditingDescription(false);
                  }}
                >
                  Save
                </button>
                <button
                  onClick={() => {
                    setDescription(card.description);
                    setEditingDescription(false);
                  }}
                >
                  Cancel
                </button>
              </div>
            </div>
          ) : (
            <button
              type="button"
              className="description-preview"
              onClick={() => setEditingDescription(true)}
            >
              {card.description || <span className="muted">Add a more detailed description…</span>}
            </button>
          )}
        </section>

        <ChecklistSection card={card} />
        <ErrorMessage error={update.error ?? move.error ?? remove.error} />
        <CommentsSection card={card} role={board.role} />
      </div>

      <aside className="card-side">
        <label className="checkbox">
          <input
            type="checkbox"
            checked={card.completedAt !== null}
            onChange={(e) => update.mutate({ completed: e.target.checked })}
          />
          Completed
        </label>

        <div className="field">
          <label htmlFor="card-due">Due date</label>
          <div className="row">
            <input
              id="card-due"
              type="date"
              value={card.dueDate ?? ''}
              onChange={(e) => update.mutate({ dueDate: e.target.value || null })}
            />
            {card.dueDate && (
              <button type="button" onClick={() => update.mutate({ dueDate: null })}>
                Clear
              </button>
            )}
          </div>
        </div>

        <AssigneePicker board={board} card={card} />
        <LabelPicker board={board} card={card} />

        <div className="field">
          <label htmlFor="card-column">Column</label>
          <select
            id="card-column"
            value={card.columnId}
            onChange={(e) => move.mutate(e.target.value)}
          >
            {board.columns.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </div>

        <button
          type="button"
          className="danger"
          onClick={() => window.confirm(`Delete "${card.title}"?`) && remove.mutate()}
        >
          Delete card
        </button>
      </aside>
    </div>
  );
}
