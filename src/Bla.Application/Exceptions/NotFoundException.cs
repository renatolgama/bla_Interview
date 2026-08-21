namespace Bla.Application.Exceptions;

// Mapped to HTTP 404 by the API's exception middleware.
public sealed class NotFoundException(string message) : Exception(message);
