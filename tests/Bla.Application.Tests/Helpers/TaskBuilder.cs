using Bla.Domain.Entities;
using Bla.Domain.Enums;

namespace Bla.Application.Tests.Helpers;

public sealed class TaskBuilder
{
    private readonly TaskItem _task = new()
    {
        Id = Guid.NewGuid(),
        Title = "Sample task",
        Description = "Sample description",
        Status = TaskItemStatus.Todo,
        CreatedAt = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc)
    };

    public static TaskBuilder For(Guid ownerId)
    {
        var builder = new TaskBuilder();
        builder._task.UserId = ownerId;
        return builder;
    }

    public TaskBuilder WithTitle(string title)
    {
        _task.Title = title;
        return this;
    }

    public TaskBuilder WithStatus(TaskItemStatus status)
    {
        _task.Status = status;
        return this;
    }

    public TaskItem Build() => _task;
}
