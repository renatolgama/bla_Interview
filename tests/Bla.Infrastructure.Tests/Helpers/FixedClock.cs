using Bla.Application.Abstractions;

namespace Bla.Infrastructure.Tests.Helpers;

public sealed class FixedClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = utcNow;
}
