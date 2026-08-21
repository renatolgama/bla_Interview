using System.Text.RegularExpressions;
using Bla.Application.Abstractions;
using Bla.Application.Contracts.Auth;
using Bla.Application.Exceptions;
using Bla.Domain.Entities;

namespace Bla.Application.Services;

public sealed partial class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IClock clock) : IAuthService
{
    private const int PasswordMinLength = 8;
    private const int NameMaxLength = 100;

    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IClock _clock = clock;

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        ValidateEmail(email);
        ValidateName(request.Name);
        ValidatePassword(request.Password);

        var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = request.Name.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = _clock.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Single generic failure for unknown email AND wrong password:
        // the response must never reveal whether the email is registered.
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        return BuildAuthResponse(user);
    }

    public async Task<UserResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException($"User '{userId}' was not found.");

        return UserResponse.FromEntity(user);
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var token = _tokenService.CreateToken(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, UserResponse.FromEntity(user));
    }

    private static string NormalizeEmail(string? email) =>
        email?.Trim().ToLowerInvariant() ?? string.Empty;

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex().IsMatch(email))
        {
            throw new ValidationException("email", "A valid email address is required.");
        }
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("name", "Name is required.");
        }

        if (name.Trim().Length > NameMaxLength)
        {
            throw new ValidationException("name", $"Name must be at most {NameMaxLength} characters.");
        }
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password) ||
            password.Length < PasswordMinLength ||
            !password.Any(char.IsLetter) ||
            !password.Any(char.IsDigit))
        {
            throw new ValidationException(
                "password",
                $"Password must be at least {PasswordMinLength} characters and contain letters and digits.");
        }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
