using Bla.Application.Contracts.Tasks;
using Bla.Domain.Enums;

namespace Bla.Application.Services;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(
        Guid userId, TaskItemStatus? status, CancellationToken cancellationToken);
    Task<TaskResponse> GetByIdAsync(
        Guid userId, Guid taskId, CancellationToken cancellationToken);
    Task<TaskResponse> CreateAsync(
        Guid userId, CreateTaskRequest request, CancellationToken cancellationToken);
    Task<TaskResponse> UpdateAsync(
        Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid userId, Guid taskId, CancellationToken cancellationToken);
}
