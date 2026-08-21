namespace Bla.Application.Exceptions;

// Mapped to HTTP 401 by the API's exception middleware. The message is
// deliberately generic: it must never reveal whether the email exists.
public sealed class InvalidCredentialsException()
    : Exception("Invalid email or password.");
