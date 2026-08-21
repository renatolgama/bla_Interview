using Bla.Application.Abstractions;
using Bla.Application.Contracts.Auth;

namespace Bla.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock) : IAuthService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IClock _clock = clock;

    public Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<UserResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken) =>
        throw new NotImplementedException();
}
