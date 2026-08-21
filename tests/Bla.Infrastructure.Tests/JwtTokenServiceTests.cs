using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Bla.Domain.Entities;
using Bla.Infrastructure.Security;
using Bla.Infrastructure.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bla.Infrastructure.Tests;

public class JwtTokenServiceTests
{
    private static readonly DateTime FixedNow = new(2026, 8, 20, 15, 0, 0, DateTimeKind.Utc);

    private readonly JwtSettings _settings = new()
    {
        Key = "unit-test-signing-key-with-at-least-32-chars!",
        Issuer = "BlaApi",
        Audience = "BlaClient",
        ExpiryMinutes = 60
    };

    private readonly User _user = new()
    {
        Id = Guid.NewGuid(),
        Email = "renato@example.com",
        Name = "Renato"
    };

    private JwtTokenService CreateSut() =>
        new(Options.Create(_settings), new FixedClock(FixedNow));

    [Fact]
    public void CreateToken_ContainsUserIdEmailAndNameClaims()
    {
        var token = CreateSut().CreateToken(_user);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == _user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "renato@example.com");
        jwt.Claims.Should().Contain(c => c.Type == "name" && c.Value == "Renato");
    }

    [Fact]
    public void CreateToken_ExpiresAfterConfiguredMinutes()
    {
        var token = CreateSut().CreateToken(_user);

        token.ExpiresAtUtc.Should().Be(FixedNow.AddMinutes(60));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);
        jwt.ValidTo.Should().Be(FixedNow.AddMinutes(60));
    }

    [Fact]
    public void CreateToken_ValidatesWithTheConfiguredKey()
    {
        var token = CreateSut().CreateToken(_user);

        var act = () => Validate(token.AccessToken, _settings.Key);

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateToken_IsRejectedWhenValidatedWithADifferentKey()
    {
        var token = CreateSut().CreateToken(_user);

        var act = () => Validate(token.AccessToken, "another-key-that-is-also-32-chars-long!!");

        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    private void Validate(string accessToken, string key)
    {
        new JwtSecurityTokenHandler().ValidateToken(accessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false // lifetime is asserted separately with the fixed clock
        }, out _);
    }
}
