using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using AltensorAccounting.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AltensorAccounting.Infrastructure.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("HTTP Request was canceled by the client: {Path}", context.Request.Path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, _env);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, IHostEnvironment env)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, title, detail, errorCode) = exception switch
        {
            ValidationException valEx => (
                (int)HttpStatusCode.BadRequest,
                "Validasiya Xətası",
                valEx.Message,
                "VALIDATION_ERROR"
            ),
            BusinessRuleException busEx => (
                (int)HttpStatusCode.BadRequest,
                "Biznes Qaydası Xətası",
                busEx.Message,
                !string.IsNullOrWhiteSpace(busEx.ErrorCode) ? busEx.ErrorCode : "BUSINESS_RULE_ERROR"
            ),
            ArgumentException argEx => (
                (int)HttpStatusCode.BadRequest,
                "Yanlış Arqument",
                argEx.Message,
                "BAD_REQUEST"
            ),
            InvalidOperationException invEx => (
                (int)HttpStatusCode.BadRequest,
                "Yolverilməz Əməliyyat",
                invEx.Message,
                "INVALID_OPERATION"
            ),
            UnauthorizedException unAuthEx => (
                (int)HttpStatusCode.Unauthorized,
                "İcazəsiz Giriş",
                unAuthEx.Message,
                "UNAUTHORIZED"
            ),
            UnauthorizedAccessException unAuthAccEx => (
                (int)HttpStatusCode.Unauthorized,
                "İcazəsiz Giriş",
                string.IsNullOrWhiteSpace(unAuthAccEx.Message) ? "Giriş qadağandır." : unAuthAccEx.Message,
                "UNAUTHORIZED"
            ),
            ForbiddenException forbEx => (
                (int)HttpStatusCode.Forbidden,
                "Qadağan Olunmuş Əməliyyat",
                forbEx.Message,
                "FORBIDDEN"
            ),
            TenantSuspendedException suspEx => (
                (int)HttpStatusCode.Forbidden,
                "Hesab Dondurulub",
                suspEx.Message,
                "TENANT_SUSPENDED"
            ),
            NotFoundException notFoundEx => (
                (int)HttpStatusCode.NotFound,
                "Resurs Tapılmadı",
                notFoundEx.Message,
                "NOT_FOUND"
            ),
            KeyNotFoundException keyEx => (
                (int)HttpStatusCode.NotFound,
                "Resurs Tapılmadı",
                keyEx.Message,
                "NOT_FOUND"
            ),
            DbUpdateConcurrencyException concEx => (
                (int)HttpStatusCode.Conflict,
                "Məlumat Ziddiyyəti Xətası",
                concEx.InnerException?.Message ?? concEx.Message,
                "CONCURRENCY_CONFLICT"
            ),
            DbUpdateException dbEx => (
                (int)HttpStatusCode.BadRequest,
                "Məlumat Bazası Xətası",
                dbEx.InnerException?.Message ?? dbEx.Message,
                "DB_UPDATE_ERROR"
            ),
            InvalidCastException castEx => (
                (int)HttpStatusCode.BadRequest,
                "Məlumat Tipi Uyğunsuzluğu",
                env.IsDevelopment()
                    ? $"Məlumat tipi uyğunsuzluğu: {castEx.Message}"
                    : "Göndərilən məlumatların formatı (tarix, rəqəm və s.) düzgün deyil. Zəhmət olmasa dəyərləri yoxlayın.",
                "DATA_TYPE_MISMATCH"
            ),
            Npgsql.PostgresException pgEx => (
                (int)HttpStatusCode.BadRequest,
                "Verilənlər Bazası Xətası",
                env.IsDevelopment()
                    ? $"PostgreSQL xətası [{pgEx.SqlState}]: {pgEx.MessageText}"
                    : "Verilənlər bazası əməliyyatında xəta baş verdi. Zəhmət olmasa administratora müraciət edin.",
                $"PG_{pgEx.SqlState}"
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Daxili Server Xətası",
                env.IsDevelopment()
                    ? $"{exception.Message} | StackTrace: {exception.StackTrace}"
                    : "Gözlənilməz bir xəta baş verdi. Zəhmət olmasa bir az sonra yenidən cəhd edin.",
                "INTERNAL_SERVER_ERROR"
            )
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (!string.IsNullOrEmpty(errorCode))
        {
            problemDetails.Extensions["code"] = errorCode;
        }

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
