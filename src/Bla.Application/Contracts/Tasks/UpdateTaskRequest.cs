using Bla.Domain.Enums;

namespace Bla.Application.Contracts.Tasks;

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    DateTime? DueDate);
