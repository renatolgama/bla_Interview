import { useCallback, useEffect, useState } from 'react';
import { createTask, deleteTask, listTasks, updateTask } from '../api/tasks';
import { ApiError } from '../api/client';
import type { CreateTaskInput, Task, TaskStatus, UpdateTaskInput } from '../types';

export type StatusFilter = TaskStatus | 'All';

const PAGE_SIZE = 9; // fills the 3-column grid evenly

export function useTasks() {
  const [tasks, setTasks] = useState<Task[]>([]);
  const [filter, setFilter] = useState<StatusFilter>('All');
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const data = await listTasks(filter === 'All' ? undefined : filter, page, PAGE_SIZE);
      // Deleting the last item of the last page leaves the current page
      // beyond the end: snap back instead of showing an empty page.
      if (data.items.length === 0 && data.totalPages > 0 && page > data.totalPages) {
        setPage(data.totalPages);
        return;
      }
      setTasks(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
      setError(null);
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not load tasks.');
    } finally {
      setIsLoading(false);
    }
  }, [filter, page]);

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
    setPage(1);
  }, []);

  const changePage = useCallback((next: number) => {
    setIsLoading(true);
    setPage(next);
  }, []);

  // Mutations reload from the server afterwards: it stays the single source
  // of truth for ordering, filtering and paging.
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

  return {
    tasks,
    filter,
    setFilter: changeFilter,
    page,
    totalPages,
    totalCount,
    setPage: changePage,
    isLoading,
    error,
    create,
    update,
    remove,
  };
}
