namespace Bla.Application.Exceptions;

// Mapped to HTTP 400 by the API's exception middleware.
public sealed class ValidationException(string field, string message)
    : Exception(message)
{
    public string Field { get; } = field;
}
