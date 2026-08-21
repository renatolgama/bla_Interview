using Bla.Application.Abstractions;
using Bla.Application.Contracts.Auth;
using Bla.Application.Exceptions;
using Bla.Application.Services;
using Bla.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Bla.Application.Tests;

public class AuthServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc);
    private static readonly AuthToken Token = new("jwt-token", FixedNow.AddMinutes(60));

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _clock.UtcNow.Returns(FixedNow);
        _tokenService.CreateToken(Arg.Any<User>()).Returns(Token);
        _sut = new AuthService(_userRepository, _passwordHasher, _tokenService, _clock);
    }

    private static RegisterRequest ValidRegister() =>
        new("Renato@Example.com", "Renato", "Passw0rd");

    // ---------- Register ----------

    [Fact]
    public async Task RegisterAsync_WithValidRequest_PersistsUserWithHashedPasswordAndNormalizedEmail()
    {
        _passwordHasher.Hash("Passw0rd").Returns("hashed-value");

        var result = await _sut.RegisterAsync(ValidRegister(), default);

        result.AccessToken.Should().Be("jwt-token");
        result.ExpiresAtUtc.Should().Be(Token.ExpiresAtUtc);
        result.User.Email.Should().Be("renato@example.com"); // normalized
        await _userRepository.Received(1).AddAsync(
            Arg.Is<User>(u =>
                u.Email == "renato@example.com" &&
                u.Name == "Renato" &&
                u.PasswordHash == "hashed-value" &&
                u.CreatedAt == FixedNow &&
                u.Id != Guid.Empty),
            default);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plainaddress")]
    [InlineData("missing@dot")]
    [InlineData("@nouser.com")]
    [InlineData("spaces in@mail.com")]
    public async Task RegisterAsync_WithInvalidEmail_ThrowsValidation(string? email)
    {
        var request = new RegisterRequest(email!, "Renato", "Passw0rd");

        var act = () => _sut.RegisterAsync(request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("email");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ab1")]          // shorter than 8
    [InlineData("abcdefgh")]     // no digit
    [InlineData("12345678")]     // no letter
    public async Task RegisterAsync_WithWeakPassword_ThrowsValidation(string? password)
    {
        var request = new RegisterRequest("valid@example.com", "Renato", password!);

        var act = () => _sut.RegisterAsync(request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("password");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_WithMissingName_ThrowsValidation(string? name)
    {
        var request = new RegisterRequest("valid@example.com", name!, "Passw0rd");

        var act = () => _sut.RegisterAsync(request, default);

        (await act.Should().ThrowAsync<ValidationException>())
            .Which.Field.Should().Be("name");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyRegistered_ThrowsConflict()
    {
        _userRepository.GetByEmailAsync("renato@example.com", default)
            .Returns(new User { Email = "renato@example.com" });

        var act = () => _sut.RegisterAsync(ValidRegister(), default);

        await act.Should().ThrowAsync<ConflictException>();
        await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    // ---------- Login ----------

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "renato@example.com",
            Name = "Renato",
            PasswordHash = "hashed-value"
        };
        _userRepository.GetByEmailAsync("renato@example.com", default).Returns(user);
        _passwordHasher.Verify("Passw0rd", "hashed-value").Returns(true);

        // Mixed-case input: login must normalize the email before lookup.
        var result = await _sut.LoginAsync(new LoginRequest("Renato@Example.com", "Passw0rd"), default);

        result.AccessToken.Should().Be("jwt-token");
        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsInvalidCredentials()
    {
        var act = () => _sut.LoginAsync(new LoginRequest("ghost@example.com", "Passw0rd"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentials()
    {
        var user = new User { Email = "renato@example.com", PasswordHash = "hashed-value" };
        _userRepository.GetByEmailAsync("renato@example.com", default).Returns(user);
        _passwordHasher.Verify("wrong", "hashed-value").Returns(false);

        var act = () => _sut.LoginAsync(new LoginRequest("renato@example.com", "wrong"), default);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task LoginAsync_FailureMessage_DoesNotRevealWhetherEmailExists()
    {
        // Same message for "unknown email" and "wrong password":
        // prevents account enumeration.
        var user = new User { Email = "renato@example.com", PasswordHash = "hashed-value" };
        _userRepository.GetByEmailAsync("renato@example.com", default).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var unknownEmail = await _sut
            .Invoking(s => s.LoginAsync(new LoginRequest("ghost@example.com", "x"), default))
            .Should().ThrowAsync<InvalidCredentialsException>();
        var wrongPassword = await _sut
            .Invoking(s => s.LoginAsync(new LoginRequest("renato@example.com", "x"), default))
            .Should().ThrowAsync<InvalidCredentialsException>();

        unknownEmail.Which.Message.Should().Be(wrongPassword.Which.Message);
    }

    // ---------- Me ----------

    [Fact]
    public async Task GetMeAsync_WhenUserExists_ReturnsProfile()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "renato@example.com", Name = "Renato" };
        _userRepository.GetByIdAsync(user.Id, default).Returns(user);

        var result = await _sut.GetMeAsync(user.Id, default);

        result.Should().Be(new UserResponse(user.Id, "renato@example.com", "Renato"));
    }

    [Fact]
    public async Task GetMeAsync_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var act = () => _sut.GetMeAsync(Guid.NewGuid(), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
