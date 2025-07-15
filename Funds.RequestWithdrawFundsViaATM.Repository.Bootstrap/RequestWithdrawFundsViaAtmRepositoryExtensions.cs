using Confluent.Kafka;
using EvDb.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class RequestWithdrawFundsViaAtmRepositoryExtensions
{
    private const string DEFAULT_STREAM_PREFIX = "ATM.Funds";

    private static EvDbStorageContext CreateContext(
                                                this IHostApplicationBuilder builder,
                                                string databaseName)
    {
        EvDbStorageContext context = new EvDbStorageContext(databaseName, builder.Environment.EnvironmentName, DEFAULT_STREAM_PREFIX);
        return context;
    }

    public static IHostApplicationBuilder AddRequestWithdrawFundsViaAtmRepository(
                                                this IHostApplicationBuilder builder,
                                                string databaseName)
    {
        IServiceCollection services = builder.Services;
        EvDbStorageContext context = builder.CreateContext(databaseName);
        services.AddEvDb()
                .AddAtmFundsFactory(storage => storage.UseMongoDBStoreForEvDbStream(), context)
                .DefaultSnapshotConfiguration(storage => storage.UseMongoDBForEvDbSnapshot());

        return builder;
    }

    public static IHostApplicationBuilder AddRequestWithdrawFundsViaAtmSink(this IHostApplicationBuilder builder,
                                                                             string databaseName,
                                                                             DateTimeOffset since,
                                                                             string topicName,
                                                                             params string[] kafkaEndpoints)
    {
        #region Validation

        if(kafkaEndpoints == null || kafkaEndpoints.Length == 0)
        {
            throw new ArgumentException("Kafka endpoints must be provided.", nameof(kafkaEndpoints));
        }

        #endregion //  Validation

        ProducerConfig config = new ()
        {
            BootstrapServers = string.Join(',', kafkaEndpoints)
        };

        return builder.AddRequestWithdrawFundsViaAtmSink(databaseName, config, since, topicName);
    }

    public static IHostApplicationBuilder AddRequestWithdrawFundsViaAtmSink(this IHostApplicationBuilder builder,
                                                                             string databaseName,
                                                                             ProducerConfig config,
                                                                             DateTimeOffset since,
                                                                             string topicName)
    {
        IServiceCollection services = builder.Services;
        EvDbStorageContext context = builder.CreateContext(databaseName);

        #region Kafka

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var producer = new ProducerBuilder<string, string>(config)
                //.SetValueSerializer(new EvDbMessageSerializer())
                .Build();
            return producer;
        });

        #endregion //  Kafka

        services.UseMongoDBChangeStream(context);
        services.AddEvDb()
                .AddSink()
                .ForMessages(context)
                    .AddFilter(EvDbMessageFilter.Create(since))
                    .AddOptions(EvDbContinuousFetchOptions.ContinueWhenEmpty)
                    .BuildHostedService()
                    .SendToKafka(topicName);

        return builder;
    }
}