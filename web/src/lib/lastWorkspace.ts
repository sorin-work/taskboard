const KEY = 'taskboard.lastWorkspace';

export const lastWorkspace = {
  get: (): string | null => {
    try {
      return localStorage.getItem(KEY);
    } catch {
      return null;
    }
  },
  set: (workspaceId: string) => {
    try {
      localStorage.setItem(KEY, workspaceId);
    } catch {
      // Private mode or storage disabled: just don't remember.
    }
  },
};
