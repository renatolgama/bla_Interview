using Bla.Application.Abstractions;
using Bla.Application.Contracts.Common;
using Bla.Application.Contracts.Tasks;
using Bla.Application.Exceptions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;

namespace Bla.Application.Services;

public sealed class TaskService(ITaskRepository taskRepository, IClock clock) : ITaskService
{
    private const int TitleMaxLength = 200;
    private const int DescriptionMaxLength = 2000;

    private readonly ITaskRepository _taskRepository = taskRepository;
    private readonly IClock _clock = clock;

    public Task<PagedResponse<TaskResponse>> GetAllAsync(
        Guid userId, TaskItemStatus? status, int page, int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public async Task<TaskResponse> GetByIdAsync(
        Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> CreateAsync(
        Guid userId, CreateTaskRequest request, CancellationToken cancellationToken)
    {
        ValidateTitle(request.Title);
        ValidateDescription(request.Description);
        ValidateDueDateForCreation(request.DueDate);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Status = TaskItemStatus.Todo,
            DueDate = request.DueDate,
            UserId = userId,
            CreatedAt = _clock.UtcNow
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        return TaskResponse.FromEntity(task);
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

        ValidateTitle(request.Title);
        ValidateDescription(request.Description);

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Status = request.Status;
        task.DueDate = request.DueDate;
        task.UpdatedAt = _clock.UtcNow;

        await _taskRepository.UpdateAsync(task, cancellationToken);
        return TaskResponse.FromEntity(task);
    }

    public async Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);
        await _taskRepository.DeleteAsync(task, cancellationToken);
    }

    // 404 for both "does not exist" and "not yours": never reveal
    // other users' resource ids.
    private async Task<TaskItem> GetOwnedTaskAsync(
        Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task is null || task.UserId != userId)
        {
            throw new NotFoundException($"Task '{taskId}' was not found.");
        }

        return task;
    }

    private static void ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException("title", "Title is required.");
        }

        if (title.Trim().Length > TitleMaxLength)
        {
            throw new ValidationException("title", $"Title must be at most {TitleMaxLength} characters.");
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Trim().Length > DescriptionMaxLength)
        {
            throw new ValidationException(
                "description", $"Description must be at most {DescriptionMaxLength} characters.");
        }
    }

    // Applies to creation only: editing an already-overdue task must not
    // force the user to change its date. Date-level comparison so "today"
    // is always a valid due date.
    private void ValidateDueDateForCreation(DateTime? dueDate)
    {
        if (dueDate.HasValue && dueDate.Value.Date < _clock.UtcNow.Date)
        {
            throw new ValidationException("dueDate", "Due date cannot be in the past.");
        }
    }
}
