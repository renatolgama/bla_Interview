namespace Bla.Application.Contracts.Tasks;

// New tasks always start as Todo, so status is not accepted on creation.
public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    DateTime? DueDate);
