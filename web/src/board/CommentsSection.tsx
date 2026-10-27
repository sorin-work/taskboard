import { useState } from 'react';
import type { FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { cardsApi } from '../api/cards';
import type { Card, Comment, Role } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Avatar } from '../components/Avatar';
import { ErrorMessage } from '../components/ErrorMessage';
import { RichText } from '../components/RichText';
import { formatRelative } from '../lib/dates';
import { queryKeys } from '../lib/queryKeys';
import { useRefreshCard } from './useRefreshCard';

export function CommentsSection({ card, role }: { card: Card; role: Role }) {
  const queryClient = useQueryClient();
  const [body, setBody] = useState('');
  const comments = useQuery({
    queryKey: queryKeys.comments(card.id),
    queryFn: () => cardsApi.comments(card.id),
  });

  const refreshCard = useRefreshCard(card.boardId, card.id);
  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.comments(card.id) });
    refreshCard();
  };

  const add = useMutation({
    mutationFn: () => cardsApi.addComment(card.id, body.trim()),
    onSuccess: () => {
      setBody('');
      refresh();
    },
  });

  function submit(event: FormEvent) {
    event.preventDefault();
    if (body.trim()) {
      add.mutate();
    }
  }

  return (
    <section className="comments">
      <h3>Comments</h3>
      <form onSubmit={submit} className="comment-form">
        <textarea
          aria-label="Write a comment"
          placeholder="Write a comment…"
          rows={2}
          maxLength={5000}
          value={body}
          onChange={(e) => setBody(e.target.value)}
        />
        {body.trim() && (
          <button className="primary" disabled={add.isPending}>
            Comment
          </button>
        )}
      </form>
      <ErrorMessage error={add.error ?? comments.error} />
      <ol className="comment-list">
        {comments.data?.map((comment) => (
          <CommentItem key={comment.id} comment={comment} role={role} onChanged={refresh} />
        ))}
      </ol>
    </section>
  );
}

function CommentItem({
  comment,
  role,
  onChanged,
}: {
  comment: Comment;
  role: Role;
  onChanged: () => void;
}) {
  const { user } = useAuth();
  const [editing, setEditing] = useState(false);
  const [body, setBody] = useState(comment.body);
  const mine = user?.id === comment.author.id;
  const canDelete = mine || role !== 'member';

  const edit = useMutation({
    mutationFn: () => cardsApi.editComment(comment.id, body.trim()),
    onSuccess: () => {
      setEditing(false);
      onChanged();
    },
  });
  const remove = useMutation({
    mutationFn: () => cardsApi.deleteComment(comment.id),
    onSuccess: onChanged,
  });

  return (
    <li className="comment">
      <Avatar id={comment.author.id} name={comment.author.displayName} />
      <div className="comment-body">
        <div className="comment-meta">
          <strong>{comment.author.displayName}</strong>
          <span className="muted small">
            {formatRelative(comment.createdAt)}
            {comment.editedAt && ' (edited)'}
          </span>
        </div>
        {editing ? (
          <form
            className="comment-form"
            onSubmit={(e) => {
              e.preventDefault();
              if (body.trim()) edit.mutate();
            }}
          >
            <textarea
              aria-label="Edit comment"
              value={body}
              rows={2}
              maxLength={5000}
              onChange={(e) => setBody(e.target.value)}
            />
            <div className="row">
              <button className="primary" disabled={edit.isPending}>
                Save
              </button>
              <button type="button" onClick={() => setEditing(false)}>
                Cancel
              </button>
            </div>
          </form>
        ) : (
          <p className="comment-text">
            <RichText text={comment.body} />
          </p>
        )}
        {!editing && (mine || canDelete) && (
          <div className="comment-actions">
            {mine && (
              <button type="button" className="link" onClick={() => setEditing(true)}>
                Edit
              </button>
            )}
            {canDelete && (
              <button
                type="button"
                className="link"
                onClick={() => window.confirm('Delete this comment?') && remove.mutate()}
              >
                Delete
              </button>
            )}
          </div>
        )}
        <ErrorMessage error={edit.error ?? remove.error} />
      </div>
    </li>
  );
}
