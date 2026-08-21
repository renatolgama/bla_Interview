using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Persistence;

// Demo data required by the exercise: one known user and a handful of tasks
// so the app can be evaluated without registering first.
public static class DbSeeder
{
    public const string DemoEmail = "demo@ballastlane.com";
    public const string DemoPassword = "Demo123!";

    public static async Task SeedAsync(
        BlaDbContext dbContext, IPasswordHasher passwordHasher, IClock clock)
    {
        if (await dbContext.Users.AnyAsync())
        {
            return;
        }

        var now = clock.UtcNow;

        var demoUser = new User
        {
            Id = Guid.NewGuid(),
            Email = DemoEmail,
            Name = "Demo User",
            PasswordHash = passwordHasher.Hash(DemoPassword),
            CreatedAt = now
        };
        dbContext.Users.Add(demoUser);

        dbContext.Tasks.AddRange(
            NewTask(demoUser.Id, "Review the exercise requirements",
                "Read the Ballast Lane PDF and map every requirement to a deliverable.",
                TaskItemStatus.Done, now.AddDays(-3), createdAt: now.AddDays(-6)),
            NewTask(demoUser.Id, "Design the database schema",
                "Users and Tasks tables, unique email index, status as string.",
                TaskItemStatus.Done, now.AddDays(-2), createdAt: now.AddDays(-5)),
            NewTask(demoUser.Id, "Implement JWT authentication",
                "Register, login, and Bearer-protected endpoints.",
                TaskItemStatus.InProgress, now.AddDays(1), createdAt: now.AddDays(-4)),
            NewTask(demoUser.Id, "Write API integration tests",
                "WebApplicationFactory over SQLite in-memory.",
                TaskItemStatus.Todo, now.AddDays(3), createdAt: now.AddDays(-3)),
            NewTask(demoUser.Id, "Prepare the demo presentation",
                "User story, architecture walkthrough, live demo, GenAI process.",
                TaskItemStatus.Todo, now.AddDays(7), createdAt: now.AddDays(-2)));

        await dbContext.SaveChangesAsync();
    }

    private static TaskItem NewTask(
        Guid userId, string title, string description,
        TaskItemStatus status, DateTime dueDate, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = description,
        Status = status,
        DueDate = dueDate,
        UserId = userId,
        CreatedAt = createdAt,
        UpdatedAt = status == TaskItemStatus.Todo ? null : createdAt.AddDays(1)
    };
}
