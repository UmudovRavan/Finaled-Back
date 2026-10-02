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
            var baseDir = AppContext.BaseDirectory;
            var logDirectory = Path.Combine(baseDir, "Logs");
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch
            {
                // Fallback to temp if BaseDirectory has no write permissions
                try
                {
                    logDirectory = Path.Combine(Path.GetTempPath(), "AltensorAccounting", "Logs");
                    Directory.CreateDirectory(logDirectory);
                }
                catch
                {
                    // Ignore
                }
            }

            var logFilePath = Path.Combine(logDirectory, "log-.txt");

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
                .MinimumLevel.Override("Npgsql", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "AltensorAccounting")
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

            Log.Logger = loggerConfig.CreateLogger();
            Log.Information("Serilog logging initialized at: {LogFilePath}", logFilePath);
        }
    }
}
