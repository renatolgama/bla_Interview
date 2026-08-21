import { useCallback, useEffect, useState } from 'react';
import { createTask, deleteTask, listTasks, updateTask } from '../api/tasks';
import { ApiError } from '../api/client';
import type { CreateTaskInput, Task, TaskStatus, UpdateTaskInput } from '../types';

export type StatusFilter = TaskStatus | 'All';

export function useTasks() {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [filter, setFilter] = useState<StatusFilter>('All');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setTasks(await listTasks(filter === 'All' ? undefined : filter));
      setError(null);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not load tasks.');
    } finally {
      setIsLoading(false);
    }
  }, [filter]);

  useEffect(() => {
    // Fetch-on-mount/filter-change is an external-system sync; every state
    // update inside load() happens after an await, never synchronously.
    // oxlint-disable-next-line react/set-state-in-effect
    void load();
  }, [load]);

  // Loading is flipped in the event that triggers the fetch (initial state
  // covers the mount), keeping effects free of synchronous state updates.
  const changeFilter = useCallback((next: StatusFilter) => {
    setIsLoading(true);
    setFilter(next);
  }, []);

  // Mutations reload from the server afterwards: it stays the single source
  // of truth for ordering and filtering.
  const create = useCallback(
    async (input: CreateTaskInput) => {
      await createTask(input);
      await load();
    },
    [load],
  );

  const update = useCallback(
    async (id: string, input: UpdateTaskInput) => {
      await updateTask(id, input);
      await load();
    },
    [load],
  );

  const remove = useCallback(
    async (id: string) => {
      await deleteTask(id);
      await load();
    },
    [load],
  );

  return { tasks, filter, setFilter: changeFilter, isLoading, error, create, update, remove };
}
