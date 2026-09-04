import { useRef, useState } from 'react';
import { useSearchParams } from 'react-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { boardsApi } from '../api/boards';
import { cardsApi } from '../api/cards';
import type { CardUpdate } from '../api/cards';
import type { Board, Card } from '../api/types';
import { ErrorMessage } from '../components/ErrorMessage';
import { LabelChip } from '../components/LabelChip';
import { Modal } from '../components/Modal';
import { RichText } from '../components/RichText';
import { queryKeys } from '../lib/queryKeys';
import { ActivityList } from './ActivityList';
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

  // Moving from the dialog drops the card at the bottom of the other column,
  // which is what people expect from "send it to Done".
  const move = useMutation({
    mutationFn: async (columnId: string) => {
      const cards = await boardsApi.cards(board.id);
      const last = cards.filter((c) => c.columnId === columnId && c.id !== card.id).at(-1);
      return cardsApi.move(card.id, columnId, last?.id ?? null);
    },
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.card(card.id), updated);
      refreshBoard();
    },
  });

  const archive = useMutation({
    mutationFn: () => (card.archivedAt ? cardsApi.restore(card.id) : cardsApi.archive(card.id)),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.card(card.id), updated);
      refreshBoard();
    },
  });

  const [, setParams] = useSearchParams();
  const duplicate = useMutation({
    mutationFn: () => cardsApi.duplicate(card.id),
    onSuccess: (copy) => {
      refreshBoard();
      setParams((current) => {
        const next = new URLSearchParams(current);
        next.set('card', copy.id);
        return next;
      });
    },
  });

  const remove = useMutation({
    mutationFn: () => cardsApi.remove(card.id),
    onSuccess: () => {
      refreshBoard();
      onClose();
    },
  });

  // Escape means "never mind": the dialog blurs the field right after, and
  // that blur must not save what was typed.
  const cancelTitle = useRef(false);

  function saveTitle() {
    if (cancelTitle.current) {
      cancelTitle.current = false;
      return;
    }
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
            } else if (e.key === 'Escape') {
              cancelTitle.current = true;
              setTitle(card.title);
            }
          }}
        />
        {card.archivedAt && (
          <p className="archived-note" role="status">
            This card is archived. Restore it to put it back on the board.
          </p>
        )}
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
            <div
              role="button"
              tabIndex={0}
              className="description-preview"
              onClick={() => setEditingDescription(true)}
              onKeyDown={(e) => e.key === 'Enter' && setEditingDescription(true)}
            >
              {card.description ? (
                <RichText text={card.description} />
              ) : (
                <span className="muted">Add a more detailed description…</span>
              )}
            </div>
          )}
        </section>

        <ChecklistSection card={card} />
        <ErrorMessage
          error={update.error ?? move.error ?? archive.error ?? duplicate.error ?? remove.error}
        />
        <CommentsSection card={card} role={board.role} />
        <CardActivity cardId={card.id} />
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

        <button type="button" onClick={() => duplicate.mutate()} disabled={duplicate.isPending}>
          Duplicate card
        </button>
        <button type="button" onClick={() => archive.mutate()} disabled={archive.isPending}>
          {card.archivedAt ? 'Restore card' : 'Archive card'}
        </button>
        <button
          type="button"
          className="danger"
          onClick={() =>
            window.confirm(`Delete "${card.title}" for good? Archiving keeps it around instead.`) &&
            remove.mutate()
          }
        >
          Delete card
        </button>
      </aside>
    </div>
  );
}

function CardActivity({ cardId }: { cardId: string }) {
  const activity = useQuery({
    queryKey: queryKeys.cardActivity(cardId),
    queryFn: () => cardsApi.activity(cardId),
  });
  return (
    <section>
      <h3>Activity</h3>
      <ErrorMessage error={activity.error} />
      {activity.data && <ActivityList items={activity.data} onCard />}
    </section>
  );
}
