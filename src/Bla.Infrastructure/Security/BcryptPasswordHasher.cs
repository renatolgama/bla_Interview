using Bla.Application.Abstractions;

namespace Bla.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    // 2^12 iterations: slow enough to resist brute force, fast enough for
    // an interactive login (~250ms).
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
