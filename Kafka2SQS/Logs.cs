using Microsoft.Extensions.Logging;
using System.Net;

namespace Kafka2SQS;

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Error, "Failed to send message to SQS. Status code: {StatusCode}")]
    public static partial void LogSQSFailure(this ILogger logger,
                                                           HttpStatusCode StatusCode);

    [LoggerMessage(LogLevel.Error, "Kafka to SQS failure")]
    public static partial void LogKafkaToSQSFailure(this ILogger logger,
                                                           Exception exception);

    [LoggerMessage(LogLevel.Debug, """
                    Sink SQS: Published to target '{target}', MessageId:{messageId} | EvDb: Status:{httpStatusCode}.
                    """)]
    public static partial void LogPublished(this ILogger logger, string target, string messageId, string httpStatusCode);

    [LoggerMessage(LogLevel.Error, """
                    Sink SQS: Failed to published to target '{target}'.
                    """)]
    public static partial void LogPublishFailed(this ILogger logger, string target, Exception exception);
}