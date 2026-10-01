using System;
using System.Threading.Tasks;
using AltensorAccounting.Contract.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Infrastructure.Middlewares;

public class TenantStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantStatusMiddleware> _logger;

    public TenantStatusMiddleware(RequestDelegate next, ILogger<TenantStatusMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService)
    {
        // Skip webhook and public paths
        if (context.Request.Path.StartsWithSegments("/internal/webhooks") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (tenantService.IsAuthenticated)
        {
            var status = tenantService.TenantStatus;
            if (string.Equals(status, "Suspended", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("[AltensorAccounting] Dayandırılmış və ya müddəti bitmiş tenant üçün sorğu bloklandı. TenantId: {TenantId}, Status: {Status}, Path: {Path}",
                    tenantService.TenantId, status, context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Tenant hesabı dayandırılıb və ya müddəti bitib. Zəhmət olmasa inzibatçı ilə əlaqə saxlayın.\", \"code\": \"TENANT_SUSPENDED\"}");
                return;
            }
        }

        await _next(context);
    }
}
