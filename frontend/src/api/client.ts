import type { Problem } from '../types';

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

const STORAGE_KEY = 'bla.auth';

export class ApiError extends Error {
  readonly status: number;
  readonly field?: string;

  constructor(problem: Problem) {
    super(problem.title);
    this.status = problem.status;
    this.field = problem.field;
  }
}

export function getStoredAuth(): { accessToken: string } | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  return raw ? JSON.parse(raw) : null;
}

export function storeAuth(auth: object): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
}

export function clearAuth(): void {
  localStorage.removeItem(STORAGE_KEY);
}

export async function request<T>(
  path: string,
  options: { method?: string; body?: unknown; auth?: boolean } = {},
): Promise<T> {
  const { method = 'GET', body, auth = true } = options;

  const headers: Record<string, string> = {};
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json';
  }
  if (auth) {
    const stored = getStoredAuth();
    if (stored) {
      headers.Authorization = `Bearer ${stored.accessToken}`;
    }
  }

  const response = await fetch(`${API_URL}${path}`, {
    method,
    headers,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  // An expired or invalid session on a protected call sends the user back
  // to the login page. Login itself (auth: false) handles its own 401.
  if (response.status === 401 && auth) {
    clearAuth();
    window.location.assign('/login');
    throw new ApiError({ status: 401, title: 'Session expired. Please sign in again.' });
  }

  if (!response.ok) {
    const problem: Problem = await response
      .json()
      .catch(() => ({ status: response.status, title: 'Unexpected error. Please try again.' }));
    throw new ApiError(problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
