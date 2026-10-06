using System;
using AltensorAccounting.Api.Extensions;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Infrastructure.Extensions;
using AltensorAccounting.Infrastructure.Middlewares;
using AltensorAccounting.Infrastructure.Services;
using AltensorAccounting.Persistence.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using AltensorAccounting.Persistence.Data;
using Microsoft.EntityFrameworkCore;

// 0. PostgreSQL Npgsql Timestamp Compatibility (solves DateTimeKind.Unspecified vs timestamptz InvalidCastException)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration matching AuthService and TMS
LoggingExtensions.ConfigureSerilog(builder.Configuration);
builder.Host.UseSerilog();

// 1. Core Services & Multi-Tenant Context
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenantService, CurrentTenantService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// 2. Persistence Layer (DbContext, Repositories, Domain Engines, App Services)
builder.Services.AddPersistenceServices(builder.Configuration);

// 3. JWT & Asymmetric JWKS Authentication
builder.Services.AddAltensorAuthentication(builder.Configuration);

// 4. Controllers & JSON Options
builder.Services.AddControllers();

// 4b. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 5. Swagger with Bearer Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Altensor Accounting API",
        Version = "v1",
        Description = "Altensor Platform - Mühasibatlıq, Satınalma, Anbar və Xəzinədarlıq Mikroservisi"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Məsələn: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 1. Global Exception Handling Middleware (Logs errors and returns ProblemDetails)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Serilog HTTP Request Logging
app.UseSerilogRequestLogging();

// 3. Auto-apply migrations & Auto-patch Company Defaults on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.CanConnect())
        {
            await db.Database.MigrateAsync();
        }
        var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DbSeeder.EnsureAllCompaniesHaveDefaultAccountsAsync(db, seederLogger);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error while applying migrations or ensuring company default accounts on startup");
    }
}

// Swagger is available across all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Altensor Accounting API v1");
});

var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardedOptions.KnownNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseMiddleware<TenantStatusMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
