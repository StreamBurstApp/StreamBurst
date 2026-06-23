using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

namespace StreamBurst.Logging
{
    public static class LoggerConfigurator
    {
        public static ILoggerFactory CreateLoggerFactory(string appDataRoot)
        {
            var logDirectory = Path.Combine(appDataRoot, "logs");
            var logFilePath = Path.Combine(logDirectory, "log-.txt");

            const string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{ShortSourceContext}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.With<ShortSourceContextEnricher>()
                .WriteTo.Console(outputTemplate: outputTemplate)
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 3,
                    outputTemplate: outputTemplate);

            var serilogLogger = loggerConfiguration.CreateLogger();

            return new SerilogLoggerFactory(serilogLogger, dispose: true);
        }
    }
}
