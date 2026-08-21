import { request } from './client';
import type { CreateTaskInput, Paged, Task, TaskStatus, UpdateTaskInput } from '../types';

export function listTasks(
  status: TaskStatus | undefined,
  page: number,
  pageSize: number,
): Promise<Paged<Task>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  if (status) {
    params.set('status', status);
  }
  return request<Paged<Task>>(`/api/tasks?${params}`);
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
