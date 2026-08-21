namespace Bla.Application.Abstractions;

// Abstracts time so date-dependent rules are testable with a frozen clock.
public interface IClock
{
    DateTime UtcNow { get; }
}
