using MatchDay.Booking.Api.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MatchDay.Booking.Api.ErrorHandling;

/// <summary>
/// Maps expected failures to proper problem responses instead of a 500: broken booking rules
/// (e.g. confirming an expired hold) become 409, malformed request bodies become 400.
/// Anything else falls through to the default exception handler.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            DomainException => (StatusCodes.Status409Conflict, "The request breaks a booking rule."),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "The request could not be read."),
            _ => (0, null),
        };

        if (statusCode == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
