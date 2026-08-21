using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(BlaDbContext dbContext) : ITaskRepository
{
    private readonly BlaDbContext _dbContext = dbContext;

    // Tracked on purpose: the entity returned here is mutated by the service
    // and handed back to UpdateAsync/DeleteAsync.
    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> GetByUserAsync(
        Guid userId, TaskItemStatus? status, CancellationToken cancellationToken)
    {
        var query = _dbContext.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
