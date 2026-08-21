using Bla.Application.Abstractions;
using Bla.Application.Exceptions;
using Bla.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(BlaDbContext dbContext) : IUserRepository
{
    private readonly BlaDbContext _dbContext = dbContext;

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _dbContext.Users.Add(user);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // The only violable constraint on this insert is the unique email
            // index (the PK is an app-generated Guid), so a save failure here
            // means a concurrent registration won the race between the
            // service-level existence check and this insert. Translate the
            // database invariant into the same 409 the service produces.
            throw new ConflictException("Email is already registered.");
        }
    }
}
