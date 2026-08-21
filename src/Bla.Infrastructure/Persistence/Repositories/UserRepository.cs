using Bla.Application.Abstractions;
using Bla.Domain.Entities;

namespace Bla.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(BlaDbContext dbContext) : IUserRepository
{
    private readonly BlaDbContext _dbContext = dbContext;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task AddAsync(User user, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
