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

    // Never cached: this read feeds updates with a tracked entity, and a
    // cached instance would be shared across concurrent requests.
    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _inner.GetByIdAsync(id, cancellationToken);

    public Task<PagedResult<TaskItem>> GetByUserAsync(
        Guid userId, TaskItemStatus? status, int page, int pageSize,
        CancellationToken cancellationToken) =>
        _cache.GetOrCreateAsync(userId, status, page, pageSize,
            () => _inner.GetByUserAsync(userId, status, page, pageSize, cancellationToken));

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await _inner.AddAsync(task, cancellationToken);
        _cache.InvalidateUser(task.UserId);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await _inner.UpdateAsync(task, cancellationToken);
        _cache.InvalidateUser(task.UserId);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await _inner.DeleteAsync(task, cancellationToken);
        _cache.InvalidateUser(task.UserId);
    }
}
