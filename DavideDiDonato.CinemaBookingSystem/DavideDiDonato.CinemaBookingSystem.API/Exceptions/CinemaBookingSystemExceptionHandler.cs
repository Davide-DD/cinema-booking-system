using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DavideDiDonato.CinemaBookingSystem.API.Exceptions;

public class CinemaBookingSystemExceptionHandler(
    ILogger<CinemaBookingSystemExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        switch (exception)
        {
            case CinemaBookingSystemApiException apiException:
                statusCode = (int)apiException.HttpStatusCode;
                break;
            // Thrown in Development when a request can't be bound, like a missing parameter or an invalid body
            case BadHttpRequestException badHttpRequestException:
                statusCode = badHttpRequestException.StatusCode;
                break;
            default:
                return false;
        }

        // Expected rejections, not failures: unhandled exceptions are logged as errors by the framework
        logger.LogInformation("Request {Method} {Path} rejected with {StatusCode}: {Detail}",
            httpContext.Request.Method, httpContext.Request.Path, statusCode, exception.Message);

        httpContext.Response.StatusCode = statusCode;
        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Detail = exception.Message
        };

        bool result = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return result;
    }
}