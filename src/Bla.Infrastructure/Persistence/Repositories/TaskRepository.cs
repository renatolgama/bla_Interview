using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Bla.Domain.Enums;

namespace Bla.Infrastructure.Persistence.Repositories;

public sealed class TaskRepository(BlaDbContext dbContext) : ITaskRepository
{
    private readonly BlaDbContext _dbContext = dbContext;

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<TaskItem>> GetByUserAsync(
        Guid userId, TaskItemStatus? status, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task UpdateAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
