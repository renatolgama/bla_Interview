using Bla.Domain.Entities;

namespace Bla.Application.Abstractions;

public sealed record AuthToken(string AccessToken, DateTime ExpiresAtUtc);

public interface ITokenService
{
    AuthToken CreateToken(User user);
}
