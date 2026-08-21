using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Bla.Infrastructure.Persistence.Repositories;
using Bla.Infrastructure.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Tests;

public class TaskRepositoryTests : IDisposable
{
    private readonly SqliteDb _db = new();
    private readonly TaskRepository _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public TaskRepositoryTests()
    {
        _sut = new TaskRepository(_db.Context);
        SeedUsers();
    }

    public void Dispose() => _db.Dispose();

    private void SeedUsers()
    {
        _db.Context.Users.AddRange(
            new User { Id = _userId, Email = "owner@example.com", Name = "Owner", PasswordHash = "x" },
            new User { Id = _otherUserId, Email = "other@example.com", Name = "Other", PasswordHash = "x" });
        _db.Context.SaveChanges();
    }

    private TaskItem NewTask(
        Guid? ownerId = null,
        string title = "Sample task",
        TaskItemStatus status = TaskItemStatus.Todo,
        DateTime? createdAt = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = "A description",
        Status = status,
        DueDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        UserId = ownerId ?? _userId,
        CreatedAt = createdAt ?? new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task AddAsync_PersistsTaskWithStatusRoundTrip()
    {
        var task = NewTask(status: TaskItemStatus.InProgress);

        await _sut.AddAsync(task, default);

        using var freshContext = _db.CreateContext();
        var persisted = await freshContext.Tasks.SingleAsync(t => t.Id == task.Id);
        persisted.Title.Should().Be("Sample task");
        persisted.Status.Should().Be(TaskItemStatus.InProgress); // string column round-trips
        persisted.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTask()
    {
        var task = NewTask();
        await _sut.AddAsync(task, default);

        var result = await _sut.GetByIdAsync(task.Id, default);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdUnknown_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyThatUsersTasks()
    {
        await _sut.AddAsync(NewTask(title: "Mine"), default);
        await _sut.AddAsync(NewTask(ownerId: _otherUserId, title: "Not mine"), default);

        var result = await _sut.GetByUserAsync(_userId, null, 1, 10, default);

        result.Items.Should().OnlyContain(t => t.UserId == _userId);
        result.Items.Should().ContainSingle(t => t.Title == "Mine");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByUserAsync_FiltersByStatus()
    {
        await _sut.AddAsync(NewTask(title: "Open", status: TaskItemStatus.Todo), default);
        await _sut.AddAsync(NewTask(title: "Finished", status: TaskItemStatus.Done), default);

        var result = await _sut.GetByUserAsync(_userId, TaskItemStatus.Done, 1, 10, default);

        result.Items.Should().ContainSingle(t => t.Title == "Finished");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsNewestFirst()
    {
        await _sut.AddAsync(NewTask(title: "Older", createdAt: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)), default);
        await _sut.AddAsync(NewTask(title: "Newer", createdAt: new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc)), default);

        var result = await _sut.GetByUserAsync(_userId, null, 1, 10, default);

        result.Items.Select(t => t.Title).Should().ContainInOrder("Newer", "Older");
    }

    [Fact]
    public async Task GetByUserAsync_SlicesPagesAndKeepsTotalCount()
    {
        await _sut.AddAsync(NewTask(title: "Oldest", createdAt: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)), default);
        await _sut.AddAsync(NewTask(title: "Middle", createdAt: new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc)), default);
        await _sut.AddAsync(NewTask(title: "Newest", createdAt: new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc)), default);

        var firstPage = await _sut.GetByUserAsync(_userId, null, 1, 2, default);
        var secondPage = await _sut.GetByUserAsync(_userId, null, 2, 2, default);

        firstPage.Items.Select(t => t.Title).Should().ContainInOrder("Newest", "Middle");
        firstPage.TotalCount.Should().Be(3);
        secondPage.Items.Should().ContainSingle(t => t.Title == "Oldest");
        secondPage.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        var task = NewTask();
        await _sut.AddAsync(task, default);

        task.Title = "Renamed";
        task.Status = TaskItemStatus.Done;
        await _sut.UpdateAsync(task, default);

        using var freshContext = _db.CreateContext();
        var persisted = await freshContext.Tasks.SingleAsync(t => t.Id == task.Id);
        persisted.Title.Should().Be("Renamed");
        persisted.Status.Should().Be(TaskItemStatus.Done);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTask()
    {
        var task = NewTask();
        await _sut.AddAsync(task, default);

        await _sut.DeleteAsync(task, default);

        using var freshContext = _db.CreateContext();
        (await freshContext.Tasks.AnyAsync(t => t.Id == task.Id)).Should().BeFalse();
    }
}
