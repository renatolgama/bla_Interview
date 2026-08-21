using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bla.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtSettings> options, IClock clock) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;
    private readonly IClock _clock = clock;

    public AuthToken CreateToken(User user)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthToken(accessToken, expiresAt);
    }
}
