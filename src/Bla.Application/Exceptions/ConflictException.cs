namespace Bla.Application.Exceptions;

// Mapped to HTTP 409 by the API's exception middleware.
public sealed class ConflictException(string message) : Exception(message);
