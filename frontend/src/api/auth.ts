import { request } from './client';
import type { AuthResponse, User } from '../types';

export function login(email: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/auth/login', {
    method: 'POST',
    body: { email, password },
    auth: false,
  });
}

export function register(email: string, name: string, password: string): Promise<AuthResponse> {
  return request<AuthResponse>('/api/auth/register', {
    method: 'POST',
    body: { email, name, password },
    auth: false,
  });
}

export function me(): Promise<User> {
  return request<User>('/api/auth/me');
}
