using Serilog.Core;
using Serilog.Events;
using System;

namespace StreamBurst.Logging
{
    public class ShortSourceContextEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Properties.TryGetValue("SourceContext", out var sourceContextValue) &&
                sourceContextValue is ScalarValue scalarValue &&
                scalarValue.Value is string sourceContext)
            {
                var shortContext = GetShortContext(sourceContext);
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ShortSourceContext", shortContext));
            }
            else
            {
                logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ShortSourceContext", "Global"));
            }
        }

        private static string GetShortContext(string sourceContext)
        {
            if (string.IsNullOrWhiteSpace(sourceContext))
                return "Global";

            int lastDot = sourceContext.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < sourceContext.Length - 1)
            {
                return sourceContext.Substring(lastDot + 1);
            }

            return sourceContext;
        }
    }
}
