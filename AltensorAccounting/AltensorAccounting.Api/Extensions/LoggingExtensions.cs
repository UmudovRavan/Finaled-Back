using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace AltensorAccounting.Api.Extensions
{
    public static class LoggingExtensions
    {
        public static void ConfigureSerilog(IConfiguration? configuration = null)
        {
            try
            {
                var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch
            {
                // Ignore if process lacks directory creation rights
            }

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("Npgsql", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "AltensorAccounting")
                .Enrich.WithProperty("MachineName", Environment.MachineName);

            try
            {
                if (configuration != null && configuration.GetSection("Serilog").Exists())
                {
                    loggerConfig.ReadFrom.Configuration(configuration);
                }
                else
                {
                    loggerConfig
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(
                            path: Path.Combine("Logs", "log-.txt"),
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");
                }

                Log.Logger = loggerConfig.CreateLogger();
            }
            catch
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .CreateLogger();
            }
        }
    }
}
