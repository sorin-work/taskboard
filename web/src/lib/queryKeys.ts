export const queryKeys = {
  workspaces: ['workspaces'] as const,
  workspace: (id: string) => ['workspaces', id] as const,
  members: (workspaceId: string) => ['workspaces', workspaceId, 'members'] as const,
  invitations: (workspaceId: string) => ['workspaces', workspaceId, 'invitations'] as const,
  boards: (workspaceId: string, includeArchived = false) =>
    ['workspaces', workspaceId, 'boards', { includeArchived }] as const,
  board: (boardId: string) => ['boards', boardId] as const,
  cards: (boardId: string, filters?: Record<string, string>) =>
    ['boards', boardId, 'cards', filters ?? {}] as const,
  archivedCards: (boardId: string) => ['boards', boardId, 'cards', 'archived'] as const,
  boardActivity: (boardId: string) => ['boards', boardId, 'activity'] as const,
  card: (cardId: string) => ['cards', cardId] as const,
  comments: (cardId: string) => ['cards', cardId, 'comments'] as const,
  cardActivity: (cardId: string) => ['cards', cardId, 'activity'] as const,
  search: (workspaceId: string, q: string) => ['workspaces', workspaceId, 'search', q] as const,
  myTasks: (params: Record<string, string>) => ['me', 'tasks', params] as const,
};
