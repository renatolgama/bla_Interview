import { useState } from 'react';
import type { FormEvent } from 'react';
import { ApiError } from '../api/client';
import type { Task, TaskStatus, UpdateTaskInput } from '../types';

interface TaskFormProps {
  task?: Task; // present = edit mode
  onSubmit: (input: UpdateTaskInput) => Promise<void>;
  onClose: () => void;
}

export function TaskForm({ task, onSubmit, onClose }: TaskFormProps) {
  const [title, setTitle] = useState(task?.title ?? '');
  const [description, setDescription] = useState(task?.description ?? '');
  const [dueDate, setDueDate] = useState(task?.dueDate?.slice(0, 10) ?? '');
  const [status, setStatus] = useState<TaskStatus>(task?.status ?? 'Todo');
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setIsSaving(true);
    try {
      await onSubmit({
        title,
        description: description.trim() === '' ? null : description,
        dueDate: dueDate === '' ? null : dueDate,
        status,
      });
      onClose();
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not save the task.');
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <div className="modal-overlay" role="dialog" aria-modal="true" aria-label={task ? 'Edit task' : 'New task'}>
      <div className="modal">
        <h2>{task ? 'Edit task' : 'New task'}</h2>
        <form onSubmit={handleSubmit}>
          <label>
            Title
            <input
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="What needs to be done?"
              maxLength={200}
              required
              autoFocus
            />
          </label>
          <label>
            Description
            <textarea
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              placeholder="Optional details"
              maxLength={2000}
              rows={3}
            />
          </label>
          <div className="form-row">
            <label>
              Due date
              <input
                type="date"
                value={dueDate}
                onChange={(event) => setDueDate(event.target.value)}
              />
            </label>
            {task && (
              <label>
                Status
                <select
                  value={status}
                  onChange={(event) => setStatus(event.target.value as TaskStatus)}
                >
                  <option value="Todo">To do</option>
                  <option value="InProgress">In progress</option>
                  <option value="Done">Done</option>
                </select>
              </label>
            )}
          </div>
          {error && <p className="form-error">{error}</p>}
          <div className="modal-actions">
            <button type="button" className="btn btn-ghost" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary" disabled={isSaving}>
              {isSaving ? 'Saving…' : task ? 'Save changes' : 'Create task'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
