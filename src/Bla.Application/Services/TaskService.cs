using Bla.Application.Abstractions;
using Bla.Application.Contracts.Tasks;
using Bla.Domain.Enums;

namespace Bla.Application.Services;

public sealed class TaskService(ITaskRepository taskRepository, IClock clock) : ITaskService
{
    private readonly ITaskRepository _taskRepository = taskRepository;
    private readonly IClock _clock = clock;

    public Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        Guid userId, TaskItemStatus? status, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskResponse> GetByIdAsync(
        Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskResponse> CreateAsync(
        Guid userId, CreateTaskRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<TaskResponse> UpdateAsync(
        Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
