import { StatusBadge } from './StatusBadge';
import type { Task, TaskStatus } from '../types';

interface TaskCardProps {
  task: Task;
  onEdit: (task: Task) => void;
  onDelete: (task: Task) => void;
  onChangeStatus: (task: Task, status: TaskStatus) => void;
}

const NEXT_ACTION: Record<TaskStatus, { label: string; next: TaskStatus }> = {
  Todo: { label: 'Start', next: 'InProgress' },
  InProgress: { label: 'Complete', next: 'Done' },
  Done: { label: 'Reopen', next: 'Todo' },
};

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  });
}

export function TaskCard({ task, onEdit, onDelete, onChangeStatus }: TaskCardProps) {
  const isOverdue =
    task.dueDate !== null && task.status !== 'Done' && new Date(task.dueDate) < new Date();
  const action = NEXT_ACTION[task.status];

  return (
    <article className={`task-card ${task.status === 'Done' ? 'task-done' : ''}`}>
      <header className="task-card-header">
        <StatusBadge status={task.status} />
        {task.dueDate && (
          <span className={`due-date ${isOverdue ? 'overdue' : ''}`}>
            {isOverdue ? 'Overdue · ' : 'Due '}
            {formatDate(task.dueDate)}
          </span>
        )}
      </header>
      <h3>{task.title}</h3>
      {task.description && <p className="task-description">{task.description}</p>}
      <footer className="task-card-actions">
        <button
          type="button"
          className="btn btn-small btn-primary"
          onClick={() => onChangeStatus(task, action.next)}
        >
          {action.label}
        </button>
        <button type="button" className="btn btn-small btn-ghost" onClick={() => onEdit(task)}>
          Edit
        </button>
        <button type="button" className="btn btn-small btn-danger" onClick={() => onDelete(task)}>
          Delete
        </button>
      </footer>
    </article>
  );
}
