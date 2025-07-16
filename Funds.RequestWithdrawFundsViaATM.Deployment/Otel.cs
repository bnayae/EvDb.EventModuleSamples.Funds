using EvDb.Core;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

internal static class OtelExtensions
{
    private const string APP_NAME = "funds:atm";

    public static WebApplicationBuilder AddOtel(this WebApplicationBuilder builder)
    {
        string otelHost = $"http://127.0.0.1";

        #region Logging

        ILoggingBuilder loggingBuilder = builder.Logging;
        loggingBuilder.AddOpenTelemetry(logging =>
        {
            var resource = ResourceBuilder.CreateDefault();
            logging.SetResourceBuilder(resource.AddService(
                            APP_NAME));  // builder.Environment.ApplicationName
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter()
                   .AddOtlpExporter("aspire", o => o.Endpoint = new Uri($"{otelHost}:18889"));
        });

        loggingBuilder.Configure(x =>
        {
            x.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
              | ActivityTrackingOptions.TraceId
              | ActivityTrackingOptions.Tags;
        });

        #endregion // Logging}

        var services = builder.Services;
        services.AddOpenTelemetry()
                    .ConfigureResource(resource =>
                                   resource.AddService(APP_NAME,
                                                    serviceInstanceId: "console-app",
                                                    autoGenerateServiceInstanceId: false)) // builder.Environment.ApplicationName
            .WithTracing(tracing =>
            {
                tracing
                        .AddEvDbInstrumentation()
                        .AddEvDbStoreInstrumentation()
                        .AddEvDbSinkKafkaInstrumentation()
                        .AddKafka2SQSInstrumentation()
                        .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                        .SetSampler<AlwaysOnSampler>()
                        .AddOtlpExporter()
                        .AddOtlpExporter("aspire", o => o.Endpoint = new Uri($"{otelHost}:18889"));
            })
            .WithMetrics(meterBuilder =>
                    meterBuilder.AddEvDbInstrumentation()
                                .AddEvDbStoreInstrumentation()
                                .AddEvDbSinkKafkaInstrumentation()
                                .AddMeter("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                                .AddOtlpExporter()
                                .AddOtlpExporter("aspire", o => o.Endpoint = new Uri($"{otelHost}:18889")));

        return builder;
    }

}
