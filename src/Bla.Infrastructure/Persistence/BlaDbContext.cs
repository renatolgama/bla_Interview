using Bla.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bla.Infrastructure.Persistence;

public class BlaDbContext(DbContextOptions<BlaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlaDbContext).Assembly);
    }
}
