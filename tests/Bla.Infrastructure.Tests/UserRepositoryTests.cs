using Bla.Domain.Entities;
using Bla.Infrastructure.Persistence.Repositories;
using Bla.Infrastructure.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Tests;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteDb _db = new();
    private readonly UserRepository _sut;

    public UserRepositoryTests()
    {
        _sut = new UserRepository(_db.Context);
    }

    public void Dispose() => _db.Dispose();

    private static User NewUser(string email = "renato@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        Name = "Renato",
        PasswordHash = "hashed-value",
        CreatedAt = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task AddAsync_PersistsUser()
    {
        var user = NewUser();

        await _sut.AddAsync(user, default);

        // Read back with a fresh context: proves it hit the database,
        // not just the change tracker.
        using var freshContext = _db.CreateContext();
        var persisted = await freshContext.Users.SingleAsync(u => u.Id == user.Id);
        persisted.Email.Should().Be("renato@example.com");
        persisted.PasswordHash.Should().Be("hashed-value");
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsDbUpdateException()
    {
        // The unique index is the last line of defense against races that
        // slip past the service-level check.
        await _sut.AddAsync(NewUser("same@example.com"), default);

        var act = () => _sut.AddAsync(NewUser("same@example.com"), default);

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsMatchingUser()
    {
        var user = NewUser("find-me@example.com");
        await _sut.AddAsync(user, default);

        var result = await _sut.GetByEmailAsync("find-me@example.com", default);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByEmailAsync_WhenEmailUnknown_ReturnsNull()
    {
        var result = await _sut.GetByEmailAsync("ghost@example.com", default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMatchingUser()
    {
        var user = NewUser();
        await _sut.AddAsync(user, default);

        var result = await _sut.GetByIdAsync(user.Id, default);

        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdUnknown_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), default);

        result.Should().BeNull();
    }
}
