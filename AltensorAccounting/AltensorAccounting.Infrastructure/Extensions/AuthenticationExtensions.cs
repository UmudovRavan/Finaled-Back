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
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, RsaSecurityKey> _keyCache = new();
    private static DateTime _lastFetched = DateTime.MinValue;
    private static readonly object _fetchLock = new();

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

                // Cached Dynamic JWKS Public Key Resolver
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    // 1. Check in-memory cache first
                    if (!string.IsNullOrEmpty(kid) && _keyCache.TryGetValue(kid, out var cachedKey))
                    {
                        return new[] { cachedKey };
                    }

                    // 2. Fetch if not in cache or cache is older than 10 minutes
                    if (_keyCache.IsEmpty || DateTime.UtcNow - _lastFetched > TimeSpan.FromMinutes(10))
                    {
                        lock (_fetchLock)
                        {
                            if (!string.IsNullOrEmpty(kid) && _keyCache.TryGetValue(kid, out var doubleCheckedKey))
                            {
                                return new[] { doubleCheckedKey };
                            }

                            try
                            {
                                var response = _httpClient.GetStringAsync(jwksUrl).GetAwaiter().GetResult();
                                using var doc = JsonDocument.Parse(response);
                                var keys = doc.RootElement.GetProperty("keys");

                                foreach (var key in keys.EnumerateArray())
                                {
                                    var currentKid = key.GetProperty("kid").GetString() ?? string.Empty;
                                    var n = key.GetProperty("n").GetString()!;
                                    var e = key.GetProperty("e").GetString()!;

                                    var rsaParams = new RSAParameters
                                    {
                                        Modulus = Base64UrlEncoder.DecodeBytes(n),
                                        Exponent = Base64UrlEncoder.DecodeBytes(e)
                                    };

                                    var rsa = RSA.Create();
                                    rsa.ImportParameters(rsaParams);
                                    var rsaKey = new RsaSecurityKey(rsa) { KeyId = currentKid };
                                    _keyCache[currentKid] = rsaKey;
                                }

                                _lastFetched = DateTime.UtcNow;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[AltensorAccounting] JWKS açarları oxunarkən xəta: {ex.Message}");
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(kid) && _keyCache.TryGetValue(kid, out var resolvedKey))
                    {
                        return new[] { resolvedKey };
                    }

                    return _keyCache.Values.Cast<SecurityKey>();
                }
            };
        });

        services.AddAuthorization();
        return services;
    }
}
