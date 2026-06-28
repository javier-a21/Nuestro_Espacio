import { api } from './client';
import type { AuthInfo } from '../types';

export const authApi = {
  register: (email: string, password: string, displayName: string) =>
    api.post<AuthInfo>('/api/auth/register', { email, password, displayName }),
  login: (email: string, password: string) =>
    api.post<AuthInfo>('/api/auth/login', { email, password }),
};
