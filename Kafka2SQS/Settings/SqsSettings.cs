namespace Kafka2SQS;

public record SqsSettings
{
    public required string Endpoint { get; set; }
    public required string QueueName { get; set; }
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string Region { get; set; }
}