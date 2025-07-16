using Kafka2SQS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafka2SQSHostedService(
        this IServiceCollection services,
        Action<KafkaSettings> kafkaOptions,
        Action<SqsSettings> sqsOptions)
    {
        services.Configure(kafkaOptions);
        services.Configure(sqsOptions);
        services.AddHostedService<Kafka2SQSHostedService>();
        return services;
    }
    public static IServiceCollection AddKafka2SQSHostedService(
        this IServiceCollection services,
        KafkaSettings kafkaSettings,
        SqsSettings sqsSettings)
    {
        services.AddHostedService<Kafka2SQSHostedService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Kafka2SQSHostedService>>();
            var result = new Kafka2SQSHostedService(logger, kafkaSettings, sqsSettings);
            return result;
        });
        return services;
    }

    public static TracerProviderBuilder AddKafka2SQSInstrumentation(this TracerProviderBuilder builder)
    {
        builder.AddSource(Telemetry.TraceName);
        return builder;
    }

}
