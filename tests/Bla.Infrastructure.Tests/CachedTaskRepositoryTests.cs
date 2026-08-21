using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Bla.Infrastructure.Caching;
using Bla.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;

namespace Bla.Infrastructure.Tests;

public class CachedTaskRepositoryTests
{
    private readonly Guid _userA = Guid.NewGuid();
    private readonly Guid _userB = Guid.NewGuid();
    private readonly CountingTaskRepository _inner = new();
    private readonly CachedTaskRepository _sut;

    public CachedTaskRepositoryTests()
    {
        var cache = new TaskListCache(new MemoryCache(new MemoryCacheOptions()));
        _sut = new CachedTaskRepository(_inner, cache);
    }

    private static TaskItem TaskFor(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Cached task",
        Status = TaskItemStatus.Todo,
        UserId = userId
    };

    [Fact]
    public async Task GetByUserAsync_SecondIdenticalCall_IsServedFromCache()
    {
        var first = await _sut.GetByUserAsync(_userA, null, 1, 9, default);
        var second = await _sut.GetByUserAsync(_userA, null, 1, 9, default);

        _inner.ListCalls.Should().Be(1);
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task GetByUserAsync_DifferentPageOrFilter_IsADifferentCacheEntry()
    {
        await _sut.GetByUserAsync(_userA, null, 1, 9, default);
        await _sut.GetByUserAsync(_userA, null, 2, 9, default);
        await _sut.GetByUserAsync(_userA, TaskItemStatus.Done, 1, 9, default);

        _inner.ListCalls.Should().Be(3);
    }

    [Fact]
    public async Task AddAsync_InvalidatesEveryCachedPageOfThatUser()
    {
        await _sut.GetByUserAsync(_userA, null, 1, 9, default);
        await _sut.GetByUserAsync(_userA, TaskItemStatus.Todo, 2, 9, default);

        await _sut.AddAsync(TaskFor(_userA), default);

        await _sut.GetByUserAsync(_userA, null, 1, 9, default);
        await _sut.GetByUserAsync(_userA, TaskItemStatus.Todo, 2, 9, default);
        _inner.ListCalls.Should().Be(4); // both pages refetched after the write
    }

    [Fact]
    public async Task UpdateAsync_InvalidatesThatUsersCache()
    {
        await _sut.GetByUserAsync(_userA, null, 1, 9, default);

        await _sut.UpdateAsync(TaskFor(_userA), default);

        await _sut.GetByUserAsync(_userA, null, 1, 9, default);
        _inner.ListCalls.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_InvalidatesThatUsersCache()
    {
        await _sut.GetByUserAsync(_userA, null, 1, 9, default);

        await _sut.DeleteAsync(TaskFor(_userA), default);

        await _sut.GetByUserAsync(_userA, null, 1, 9, default);
        _inner.ListCalls.Should().Be(2);
    }

    [Fact]
    public async Task WritesForOneUser_DoNotInvalidateAnotherUsersCache()
    {
        await _sut.GetByUserAsync(_userA, null, 1, 9, default); // 1
        await _sut.GetByUserAsync(_userB, null, 1, 9, default); // 2

        await _sut.AddAsync(TaskFor(_userB), default);

        await _sut.GetByUserAsync(_userA, null, 1, 9, default); // still cached
        await _sut.GetByUserAsync(_userB, null, 1, 9, default); // refetched -> 3
        _inner.ListCalls.Should().Be(3);
    }

    [Fact]
    public async Task GetByIdAsync_IsNeverCached()
    {
        // The by-id read feeds updates (tracked entity) — caching it would
        // hand the same instance to concurrent requests. Always pass through.
        await _sut.GetByIdAsync(Guid.NewGuid(), default);
        await _sut.GetByIdAsync(Guid.NewGuid(), default);

        _inner.GetByIdCalls.Should().Be(2);
    }

    [Fact]
    public async Task WriteCalls_AreForwardedToTheInnerRepository()
    {
        var task = TaskFor(_userA);

        await _sut.AddAsync(task, default);
        await _sut.UpdateAsync(task, default);
        await _sut.DeleteAsync(task, default);

        _inner.AddCalls.Should().Be(1);
        _inner.UpdateCalls.Should().Be(1);
        _inner.DeleteCalls.Should().Be(1);
    }

    // Hand-rolled fake: counts calls and returns a fresh PagedResult per
    // list call so reference equality proves cache hits.
    private sealed class CountingTaskRepository : ITaskRepository
    {
        public int ListCalls;
        public int GetByIdCalls;
        public int AddCalls;
        public int UpdateCalls;
        public int DeleteCalls;

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            GetByIdCalls++;
            return Task.FromResult<TaskItem?>(null);
        }

        public Task<PagedResult<TaskItem>> GetByUserAsync(
            Guid userId, TaskItemStatus? status, int page, int pageSize,
            CancellationToken cancellationToken)
        {
            ListCalls++;
            return Task.FromResult(new PagedResult<TaskItem>([], 0));
        }

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            return Task.CompletedTask;
        }
    }
}
