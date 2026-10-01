using System;
using AltensorAccounting.Api.Extensions;
using AltensorAccounting.Contract.Services;
using AltensorAccounting.Infrastructure.Extensions;
using AltensorAccounting.Infrastructure.Middlewares;
using AltensorAccounting.Infrastructure.Services;
using AltensorAccounting.Persistence.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Altensor Accounting API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<TenantStatusMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
