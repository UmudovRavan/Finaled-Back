using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Presentation.ExceptionHandler
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Full stack trace-i log-la ki production-da debug etmək asan olsun
            _logger.LogError(
                exception,
                "Gözlənilməz xəta baş verdi: {ExceptionType} — {Message}\nStackTrace: {StackTrace}",
                exception.GetType().Name,
                exception.Message,
                exception.StackTrace);

            int statusCode;
            string title;
            string detail;

            switch (exception)
            {
                case ArgumentException argEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    title = "Bad Request";
                    detail = argEx.Message;
                    break;
                case InvalidOperationException invEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    title = "Bad Request";
                    detail = invEx.Message;
                    break;
                case KeyNotFoundException knfEx:
                    statusCode = StatusCodes.Status404NotFound;
                    title = "Not Found";
                    detail = knfEx.Message;
                    break;
                case UnauthorizedAccessException unAuthEx:
                    statusCode = StatusCodes.Status403Forbidden;
                    title = "Forbidden";
                    detail = unAuthEx.Message;
                    break;
                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    title = "Internal Server Error";
                    detail = "Serverdə gözlənilməz xəta baş verdi. Zəhmət olmasa bir az sonra yenidən cəhd edin.";
                    break;
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            var json = JsonSerializer.Serialize(problemDetails);
            await httpContext.Response.WriteAsync(json, cancellationToken);

            return true;
        }
    }
}
