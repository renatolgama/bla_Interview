import { request } from './client';
import type { CreateTaskInput, Task, TaskStatus, UpdateTaskInput } from '../types';

export function listTasks(status?: TaskStatus): Promise<Task[]> {
  const query = status ? `?status=${status}` : '';
  return request<Task[]>(`/api/tasks${query}`);
}

export function createTask(input: CreateTaskInput): Promise<Task> {
  return request<Task>('/api/tasks', { method: 'POST', body: input });
}

export function updateTask(id: string, input: UpdateTaskInput): Promise<Task> {
  return request<Task>(`/api/tasks/${id}`, { method: 'PUT', body: input });
}

export function deleteTask(id: string): Promise<void> {
  return request<void>(`/api/tasks/${id}`, { method: 'DELETE' });
}
