using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        string detail = exception.Message;
        switch (exception)
        {
            case CinemaBookingSystemApiException apiException:
                statusCode = (int)apiException.HttpStatusCode;
                break;
            // Thrown in Development when a request can't be bound, like a missing parameter or an invalid body
            case BadHttpRequestException badHttpRequestException:
                statusCode = badHttpRequestException.StatusCode;
                break;
            // Thrown when another request deletes a record between reading and saving it, like a double click on delete
            case DbUpdateConcurrencyException:
                statusCode = StatusCodes.Status404NotFound;
                detail = "The record was deleted by another request";
                break;
            default:
                return false;
        }

        // Expected rejections, not failures: unhandled exceptions are logged as errors by the framework
        logger.LogInformation("Request {Method} {Path} rejected with {StatusCode}: {Detail}",
            httpContext.Request.Method, httpContext.Request.Path, statusCode, detail);

        httpContext.Response.StatusCode = statusCode;
        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Detail = detail
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