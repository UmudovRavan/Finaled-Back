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

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Internal Server Error",
                Detail = "Serverdə gözlənilməz xəta baş verdi. Zəhmət olmasa bir az sonra yenidən cəhd edin.",
                Instance = httpContext.Request.Path
            };

            var json = JsonSerializer.Serialize(problemDetails);
            await httpContext.Response.WriteAsync(json, cancellationToken);

            return true;
        }
    }
}
