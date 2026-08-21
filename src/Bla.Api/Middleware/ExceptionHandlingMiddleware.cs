using Bla.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Bla.Api.Middleware;

// Single translation point from Application exceptions to RFC 7807
// ProblemDetails responses. The Application layer stays HTTP-free.
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, field) = exception switch
        {
            ValidationException validation =>
                (StatusCodes.Status400BadRequest, validation.Message, validation.Field),
            InvalidCredentialsException credentials =>
                (StatusCodes.Status401Unauthorized, credentials.Message, null),
            NotFoundException notFound =>
                (StatusCodes.Status404NotFound, notFound.Message, null),
            ConflictException conflict =>
                (StatusCodes.Status409Conflict, conflict.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", (string?)null)
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Instance = context.Request.Path
        };

        if (field is not null)
        {
            problem.Extensions["field"] = field;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
