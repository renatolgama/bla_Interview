using Bla.Infrastructure.Security;
using FluentAssertions;

namespace Bla.Infrastructure.Tests;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_DoesNotReturnThePlaintextPassword()
    {
        var hash = _sut.Hash("Passw0rd");

        hash.Should().NotBe("Passw0rd");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Hash_UsesWorkFactor12()
    {
        // BCrypt hashes embed their cost: "$2a$12$...". Pinning it prevents
        // a silent downgrade of hashing strength.
        var hash = _sut.Hash("Passw0rd");

        hash.Should().MatchRegex(@"^\$2[aby]\$12\$");
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // Different salts per hash: equal passwords must not produce
        // equal hashes.
        var first = _sut.Hash("Passw0rd");
        var second = _sut.Hash("Passw0rd");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("Passw0rd");

        _sut.Verify("Passw0rd", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("Passw0rd");

        _sut.Verify("wrong-password", hash).Should().BeFalse();
    }
}
