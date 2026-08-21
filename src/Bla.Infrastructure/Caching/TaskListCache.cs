using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Bla.Infrastructure.Caching;

// Singleton. Short-TTL read-through cache for per-user task pages.
// Invalidation is per user: cancelling the user's token evicts every page
// at once, which copes with unbounded page/pageSize/filter combinations.
public sealed class TaskListCache(IMemoryCache memoryCache)
{
    private readonly IMemoryCache _memoryCache = memoryCache;

    public Task<PagedResult<TaskItem>> GetOrCreateAsync(
        Guid userId, TaskItemStatus? status, int page, int pageSize,
        Func<Task<PagedResult<TaskItem>>> factory) =>
        throw new NotImplementedException();

    public void InvalidateUser(Guid userId) =>
        throw new NotImplementedException();
}
