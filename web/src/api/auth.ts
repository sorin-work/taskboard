import { api } from './client';
import type { AuthResponse, User } from './types';

export const authApi = {
  login: (email: string, password: string) =>
    api<AuthResponse>('/auth/login', { method: 'POST', body: { email, password } }),
  signup: (email: string, password: string, displayName: string) =>
    api<AuthResponse>('/auth/signup', { method: 'POST', body: { email, password, displayName } }),
  me: () => api<User>('/auth/me'),
};
