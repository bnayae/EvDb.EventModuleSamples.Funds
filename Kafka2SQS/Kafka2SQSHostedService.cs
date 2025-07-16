using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Confluent.Kafka;
using Microsoft.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Kafka2SQS;

public class Kafka2SQSHostedService : BackgroundService
{
    private readonly AmazonSQSClient _sqsClient;
    private readonly ILogger<Kafka2SQSHostedService> _logger;
    private readonly SqsSettings _sqsSettings;
    private readonly KafkaSettings _kafkaSettings;
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;


    public Kafka2SQSHostedService(
        ILogger<Kafka2SQSHostedService> logger,
        IOptions<KafkaSettings> kafkaOptions,
        IOptions<SqsSettings> sqsOptions) : this(logger, kafkaOptions.Value, sqsOptions.Value)
    {
    }

    public Kafka2SQSHostedService(
        ILogger<Kafka2SQSHostedService> logger,
        KafkaSettings kafkaSettings,
        SqsSettings sqsSettings)
    {
        var config = new AmazonSQSConfig
        {
            ServiceURL = sqsSettings.Endpoint,
            // AuthenticationRegion = REGION
            UseHttp = true
        };
        BasicAWSCredentials credentials = new BasicAWSCredentials(sqsSettings.AccessKeyId,
            sqsSettings.SecretAccessKey);
        _sqsClient = new AmazonSQSClient(credentials, config);
        _logger = logger;
        _sqsSettings = sqsSettings;
        _kafkaSettings = kafkaSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _sqsClient.GetOrCreateQueueAsync(_sqsSettings.QueueName, TimeSpan.FromDays(3), _logger, stoppingToken);
        var config = new ConsumerConfig
        {
            BootstrapServers = string.Join(',', _kafkaSettings.Endpoints),
            GroupId = "Kafka2SQSGroup",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_kafkaSettings.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Yield(); // Yield to avoid blocking the thread

            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult == null)
                    continue;

                #region  using var activity = Telemetry.Trace.StartActivity("Kafka Message Consumed", ActivityKind.Consumer)

                using var activity = Telemetry.Trace.StartActivity("Kafka -> SQS", ActivityKind.Consumer);

                // Extract parent context from Kafka headers
                var parentContext = Propagator.Extract(default, consumeResult.Message.Headers, static (headers, key) =>
                {
                    var header = headers.FirstOrDefault(h => h.Key == key);
                    return header is null ? Enumerable.Empty<string>() : [Encoding.UTF8.GetString(header.GetValueBytes())];
                });
                var parentLink = new ActivityLink(parentContext.ActivityContext);
                activity?.AddLink(parentLink);

                #endregion //   using var activity = Telemetry.Trace.StartActivity("Kafka Message Consumed", ActivityKind.Consumer)
                using var kafkaActivity = Telemetry.Trace.StartActivity("Kafka Message Consumed", ActivityKind.Consumer);

                var response = await PublishAsync(
                    _sqsSettings.QueueName,
                    consumeResult.Message.Value,
                    null, // No specific serializer options
                    stoppingToken);

                #region consumer.Commit(consumeResult)

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    // Commit only after successful send
                    consumer.Commit(consumeResult);
                }
                else
                {
                    _logger.LogSQSFailure(response.HttpStatusCode);
                    kafkaActivity?.SetStatus(ActivityStatusCode.Error);
                }

                #endregion //  consumer.Commit(consumeResult)

            }
            #region Error Handling

            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogKafkaToSQSFailure(ex);
                consumer.Subscribe(_kafkaSettings.Topic); // re-listen to the topic
            }

            #endregion //  Error Handling
        }

        consumer.Close();
    }

    public override void Dispose()
    {
        base.Dispose();
        _sqsClient.Dispose();
    }

    private async Task<SendMessageResponse> PublishAsync(string target,
                                                    string message,
                                                    JsonSerializerOptions? serializerOptions,
                                                    CancellationToken cancellationToken)
    {

        using var activity = Telemetry.Trace.StartActivity("Sending to SQS", ActivityKind.Producer);

        #region MessageAttributeValue messageAttributes = OTEL Context

        // Create SNS message attributes dictionary
        var messageAttributes = new Dictionary<string, MessageAttributeValue>();

        // Get the current propagation context
        var propagationContext = new PropagationContext(
            activity?.Context ?? default,
            Baggage.Create()
        );

        Propagator.Inject(propagationContext, messageAttributes, (
                    Dictionary<string, MessageAttributeValue> carrier,
                    string key,
                    string value) =>
                            {
                                carrier[key] = new MessageAttributeValue
                                {
                                    DataType = "String",
                                    StringValue = value
                                };
                            });

        #endregion // MessageAttributeValue messageAttributes = OTEL Context

        #region var request = new SendMessageRequest(..)

        var request = new SendMessageRequest
        {
            QueueUrl = target,
            MessageBody = message,
            MessageAttributes = messageAttributes,
        };

        #endregion //  var request = new SendMessageRequest(..)
        try
        {
            SendMessageResponse response = await _sqsClient.SendMessageAsync(request, cancellationToken);
            _logger.LogPublished(target, response.MessageId, response.HttpStatusCode.ToString());
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogPublishFailed(target, ex);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}

