using System;
using System.Threading.Tasks;
using AltensorAccounting.Contract.Services;
using Microsoft.AspNetCore.Http;

namespace AltensorAccounting.Infrastructure.Middlewares;

public class TenantStatusMiddleware
{
    private readonly RequestDelegate _next;

    public TenantStatusMiddleware(RequestDelegate next)
    {
        _next = next;
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
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"Tenant hesabı dayandırılıb və ya müddəti bitib. Zəhmət olmasa inzibatçı ilə əlaqə saxlayın.\"}");
                return;
            }
        }

        await _next(context);
    }
}
