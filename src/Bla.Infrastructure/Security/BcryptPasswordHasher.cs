using Bla.Application.Abstractions;

namespace Bla.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        throw new NotImplementedException();

    public bool Verify(string password, string passwordHash) =>
        throw new NotImplementedException();
}
