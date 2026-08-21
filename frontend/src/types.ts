export type TaskStatus = 'Todo' | 'InProgress' | 'Done';

export interface Task {
  id: string;
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDate: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface User {
  id: string;
  email: string;
  name: string;
}

export interface AuthResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: User;
}

export interface CreateTaskInput {
  title: string;
  description: string | null;
  dueDate: string | null;
}

export interface UpdateTaskInput extends CreateTaskInput {
  status: TaskStatus;
}

// RFC 7807 problem details returned by the API on errors.
export interface Problem {
  status: number;
  title: string;
  field?: string;
}
