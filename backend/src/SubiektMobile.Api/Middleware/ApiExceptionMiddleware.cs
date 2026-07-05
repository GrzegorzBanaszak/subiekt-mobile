using Microsoft.AspNetCore.Mvc;
using SubiektMobile.Application.Identity;

namespace SubiektMobile.Api.Middleware;

public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                RequestValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
                SignInFailedException => (StatusCodes.Status401Unauthorized, "Sign-in failed"),
                AccessDeniedException => (StatusCodes.Status403Forbidden, "Access denied"),
                ResourceNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ResourceConflictException => (StatusCodes.Status409Conflict, "Resource conflict"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected server error")
            };

            if (status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
                Instance = context.Request.Path
            });
        }
    }
}
