import { useEffect, useRef, useState } from 'react';
import type { CardFilters } from '../api/boards';
import type { Board } from '../api/types';
import { activeFilterCount, DUE_OPTIONS } from '../lib/filters';

interface FilterBarProps {
  board: Board;
  filters: CardFilters;
  onChange: (filters: CardFilters) => void;
}

export function FilterBar({ board, filters, onChange }: FilterBarProps) {
  const [q, setQ] = useState(filters.q ?? '');
  const searchRef = useRef<HTMLInputElement>(null);

  // "/" jumps to the search box, like on most sites with one.
  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      const target = event.target as HTMLElement | null;
      if (event.key !== '/' || target?.closest('input, textarea, select, [contenteditable]')) {
        return;
      }
      event.preventDefault();
      searchRef.current?.focus();
    }
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, []);

  // Wait for a pause in typing before filtering.
  useEffect(() => {
    if (q === (filters.q ?? '')) {
      return;
    }
    const timer = setTimeout(() => onChange({ ...filters, q }), 250);
    return () => clearTimeout(timer);
  }, [q, filters, onChange]);

  const set = (key: keyof CardFilters, value: string) => onChange({ ...filters, [key]: value });
  const toggle = (key: keyof CardFilters, value: string) =>
    set(key, filters[key] === value ? '' : value);

  return (
    <div className="filter-bar" role="search">
      <div className="quick-filters" role="group" aria-label="Quick filters">
        <button
          type="button"
          className="chip"
          aria-pressed={filters.assignee === 'me'}
          onClick={() => toggle('assignee', 'me')}
        >
          My cards
        </button>
        <button
          type="button"
          className="chip"
          aria-pressed={filters.due === 'week'}
          onClick={() => toggle('due', 'week')}
        >
          Due this week
        </button>
        <button
          type="button"
          className="chip"
          aria-pressed={filters.due === 'overdue'}
          onClick={() => toggle('due', 'overdue')}
        >
          Overdue
        </button>
      </div>
      <input
        ref={searchRef}
        type="search"
        aria-label="Search cards"
        placeholder="Search cards…  /"
        value={q}
        onChange={(e) => setQ(e.target.value)}
      />
      <select
        aria-label="Assignee"
        value={filters.assignee ?? ''}
        onChange={(e) => set('assignee', e.target.value)}
      >
        <option value="">Anyone</option>
        <option value="me">Assigned to me</option>
        <option value="none">Unassigned</option>
        {board.members.map((m) => (
          <option key={m.userId} value={m.userId}>
            {m.displayName}
          </option>
        ))}
      </select>
      <select
        aria-label="Label"
        value={filters.label ?? ''}
        onChange={(e) => set('label', e.target.value)}
      >
        <option value="">Any label</option>
        {board.labels.map((l) => (
          <option key={l.id} value={l.id}>
            {l.name}
          </option>
        ))}
      </select>
      <select
        aria-label="Due"
        value={filters.due ?? ''}
        onChange={(e) => set('due', e.target.value)}
      >
        <option value="">Any due date</option>
        {DUE_OPTIONS.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </select>
      {activeFilterCount(filters) > 0 && (
        <button
          type="button"
          onClick={() => {
            setQ('');
            onChange({});
          }}
        >
          Clear filters
        </button>
      )}
    </div>
  );
}
