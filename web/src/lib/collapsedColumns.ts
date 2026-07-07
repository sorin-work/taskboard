const key = (boardId: string) => `taskboard.collapsed.${boardId}`;

/** Which columns someone has folded away on a board, remembered per browser. */
export const collapsedColumns = {
  get(boardId: string): string[] {
    try {
      const value = JSON.parse(localStorage.getItem(key(boardId)) ?? '[]') as unknown;
      return Array.isArray(value) ? value.filter((v): v is string => typeof v === 'string') : [];
    } catch {
      return [];
    }
  },
  set(boardId: string, columnIds: string[]) {
    try {
      if (columnIds.length === 0) {
        localStorage.removeItem(key(boardId));
      } else {
        localStorage.setItem(key(boardId), JSON.stringify(columnIds));
      }
    } catch {
      // Storage unavailable: collapsing just won't stick.
    }
  },
  toggle(boardId: string, columnId: string): string[] {
    const current = collapsedColumns.get(boardId);
    const next = current.includes(columnId)
      ? current.filter((id) => id !== columnId)
      : [...current, columnId];
    collapsedColumns.set(boardId, next);
    return next;
  },
};
