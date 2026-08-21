using Bla.Domain.Entities;
using Bla.Domain.Enums;

namespace Bla.Application.Abstractions;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<TaskItem>> GetByUserAsync(
        Guid userId, TaskItemStatus? status, int page, int pageSize,
        CancellationToken cancellationToken);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);
    Task UpdateAsync(TaskItem task, CancellationToken cancellationToken);
    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
