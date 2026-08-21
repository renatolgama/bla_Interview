using Bla.Domain.Entities;
using Bla.Domain.Enums;

namespace Bla.Application.Contracts.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static TaskResponse FromEntity(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status,
            task.DueDate, task.CreatedAt, task.UpdatedAt);
}
