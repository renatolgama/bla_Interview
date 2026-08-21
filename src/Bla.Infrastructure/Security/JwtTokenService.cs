using Bla.Application.Abstractions;
using Bla.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Bla.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtSettings> options, IClock clock) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;
    private readonly IClock _clock = clock;

    public AuthToken CreateToken(User user) =>
        throw new NotImplementedException();
}
