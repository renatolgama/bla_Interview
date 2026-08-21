using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Bla.Infrastructure.Caching;

namespace Bla.Infrastructure.Persistence.Repositories;

// Decorator over the EF repository: caches the paged list reads and
// invalidates the owner's cache on every write. Wired up in DI only —
// the Application layer never learns that caching exists.
public sealed class CachedTaskRepository(ITaskRepository inner, TaskListCache cache)
    : ITaskRepository
{
    private readonly ITaskRepository _inner = inner;
    private readonly TaskListCache _cache = cache;

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<PagedResult<TaskItem>> GetByUserAsync(
        Guid userId, TaskItemStatus? status, int page, int pageSize,
        CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
