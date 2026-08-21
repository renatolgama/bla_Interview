using Bla.Application.Abstractions;
using Bla.Infrastructure.Caching;
using Bla.Infrastructure.Persistence;
using Bla.Infrastructure.Persistence.Repositories;
using Bla.Infrastructure.Security;
using Bla.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bla.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BlaDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUserRepository, UserRepository>();

        // ITaskRepository resolves to the caching decorator wrapping the EF
        // repository — swapping or removing the cache is a one-line change.
        services.AddMemoryCache();
        services.AddSingleton<TaskListCache>();
        services.AddScoped<TaskRepository>();
        services.AddScoped<ITaskRepository>(provider => new CachedTaskRepository(
            provider.GetRequiredService<TaskRepository>(),
            provider.GetRequiredService<TaskListCache>()));

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
