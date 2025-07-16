namespace Kafka2SQS;

public record KafkaSettings
{
    public required string Topic { get; set; }
    public required string[] Endpoints { get; set; }
}
