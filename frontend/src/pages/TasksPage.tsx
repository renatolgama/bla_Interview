import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { TaskCard } from '../components/TaskCard';
import { TaskForm } from '../components/TaskForm';
import { useTasks } from '../hooks/useTasks';
import type { StatusFilter } from '../hooks/useTasks';
import { ApiError } from '../api/client';
import type { Task, TaskStatus, UpdateTaskInput } from '../types';

const FILTERS: { value: StatusFilter; label: string }[] = [
  { value: 'All', label: 'All' },
  { value: 'Todo', label: 'To do' },
  { value: 'InProgress', label: 'In progress' },
  { value: 'Done', label: 'Done' },
];

export function TasksPage() {
  const { user, logout } = useAuth();
  const { tasks, filter, setFilter, isLoading, error, create, update, remove } = useTasks();
  const [isCreating, setIsCreating] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function handleChangeStatus(task: Task, status: TaskStatus) {
    setActionError(null);
    try {
      await update(task.id, {
        title: task.title,
        description: task.description,
        dueDate: task.dueDate,
        status,
      });
    } catch (caught) {
      setActionError(caught instanceof ApiError ? caught.message : 'Could not update the task.');
    }
  }

  async function handleDelete(task: Task) {
    if (!window.confirm(`Delete "${task.title}"?`)) {
      return;
    }
    setActionError(null);
    try {
      await remove(task.id);
    } catch (caught) {
      setActionError(caught instanceof ApiError ? caught.message : 'Could not delete the task.');
    }
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <span className="brand">BLA Tasks</span>
        <div className="header-right">
          <span className="user-name">Hi, {user?.name}</span>
          <button type="button" className="btn btn-ghost btn-small" onClick={logout}>
            Sign out
          </button>
        </div>
      </header>

      <main className="app-main">
        <div className="toolbar">
          <div className="filters" role="tablist" aria-label="Filter tasks by status">
            {FILTERS.map(({ value, label }) => (
              <button
                key={value}
                type="button"
                role="tab"
                aria-selected={filter === value}
                className={`chip ${filter === value ? 'chip-active' : ''}`}
                onClick={() => setFilter(value)}
              >
                {label}
              </button>
            ))}
          </div>
          <button type="button" className="btn btn-primary" onClick={() => setIsCreating(true)}>
            + New task
          </button>
        </div>

        {(error || actionError) && <p className="page-error">{error ?? actionError}</p>}

        {isLoading ? (
          <p className="empty-state">Loading tasks…</p>
        ) : tasks.length === 0 ? (
          <p className="empty-state">
            {filter === 'All' ? 'No tasks yet. Create your first one!' : 'No tasks with this status.'}
          </p>
        ) : (
          <section className="task-grid" aria-label="Task list">
            {tasks.map((task) => (
              <TaskCard
                key={task.id}
                task={task}
                onEdit={setEditingTask}
                onDelete={handleDelete}
                onChangeStatus={handleChangeStatus}
              />
            ))}
          </section>
        )}
      </main>

      {isCreating && (
        <TaskForm
          onClose={() => setIsCreating(false)}
          onSubmit={async (input: UpdateTaskInput) =>
            create({ title: input.title, description: input.description, dueDate: input.dueDate })
          }
        />
      )}
      {editingTask && (
        <TaskForm
          task={editingTask}
          onClose={() => setEditingTask(null)}
          onSubmit={(input) => update(editingTask.id, input)}
        />
      )}
    </div>
  );
}
