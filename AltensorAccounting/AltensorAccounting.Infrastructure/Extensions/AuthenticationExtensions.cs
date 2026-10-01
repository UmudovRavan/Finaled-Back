using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AltensorAccounting.Infrastructure.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAltensorAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "AltensorAuthService";
        var audience = configuration["Jwt:Audience"] ?? "AltensorPlatform";
        var jwksUrl = configuration["AuthService:JwksEndpoint"] ?? "https://localhost:7196/.well-known/jwks.json";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateIssuerSigningKey = true,

                // Dynamic JWKS Public Key Resolver from AltensorAuthService
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    try
                    {
                        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                        var response = httpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
                        using var doc = JsonDocument.Parse(response);
                        var keys = doc.RootElement.GetProperty("keys");

                        foreach (var key in keys.EnumerateArray())
                        {
                            var currentKid = key.GetProperty("kid").GetString();
                            if (currentKid == kid || string.IsNullOrEmpty(kid))
                            {
                                var n = key.GetProperty("n").GetString()!;
                                var e = key.GetProperty("e").GetString()!;

                                var rsaParams = new RSAParameters
                                {
                                    Modulus = Base64UrlEncoder.DecodeBytes(n),
                                    Exponent = Base64UrlEncoder.DecodeBytes(e)
                                };

                                var rsa = RSA.Create();
                                rsa.ImportParameters(rsaParams);
                                return new[] { new RsaSecurityKey(rsa) { KeyId = currentKid } };
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AltensorAccounting] JWKS açarları oxunarkən xəta: {ex.Message}");
                    }

                    return Enumerable.Empty<SecurityKey>();
                }
            };
        });

        services.AddAuthorization();
        return services;
    }
}
