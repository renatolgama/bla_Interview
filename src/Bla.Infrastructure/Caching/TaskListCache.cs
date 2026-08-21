using System.Collections.Concurrent;
using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Bla.Infrastructure.Caching;

// Singleton. Short-TTL read-through cache for per-user task pages.
// Invalidation is per user: cancelling the user's token evicts every page
// at once, which copes with unbounded page/pageSize/filter combinations.
public sealed class TaskListCache(IMemoryCache memoryCache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _userTokens = new();

    public async Task<PagedResult<TaskItem>> GetOrCreateAsync(
        Guid userId, TaskItemStatus? status, int page, int pageSize,
        Func<Task<PagedResult<TaskItem>>> factory)
    {
        var key = $"tasks:{userId}:{status?.ToString() ?? "all"}:{page}:{pageSize}";

        if (_memoryCache.TryGetValue(key, out PagedResult<TaskItem>? cached) && cached is not null)
        {
            return cached;
        }

        var result = await factory();

        // If an invalidation lands while the factory runs, the token below is
        // already cancelled and the entry expires immediately — stale data is
        // never served.
        var userToken = _userTokens.GetOrAdd(userId, _ => new CancellationTokenSource());
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = Ttl };
        options.AddExpirationToken(new CancellationChangeToken(userToken.Token));
        _memoryCache.Set(key, result, options);

        return result;
    }

    public void InvalidateUser(Guid userId)
    {
        if (_userTokens.TryRemove(userId, out var tokenSource))
        {
            tokenSource.Cancel();
            tokenSource.Dispose();
        }
    }
}
