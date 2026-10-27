using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace DavideDiDonato.CinemaBookingSystem.API.Exceptions;

public class CinemaBookingSystemApiException(HttpStatusCode httpStatusCode, string message)
    : Exception(message)
{
    public HttpStatusCode HttpStatusCode { get; } = httpStatusCode;
}