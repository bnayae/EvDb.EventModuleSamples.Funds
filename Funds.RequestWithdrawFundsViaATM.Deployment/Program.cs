using Kafka2SQS;
using Microsoft.Extensions.DependencyInjection;

const string DATABASE_NAME = "tests";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddOtel();

var configSection = builder.Configuration.GetSection("RequestWithdrawFundsViaAtm");
string dbName = configSection.GetValue<string>("DatabaseName") ?? DATABASE_NAME;
KafkaSettings kafkaSettings = configSection.GetSection("Kafka").Get<KafkaSettings>();
string[] kafkaEndpoints = kafkaSettings.Endpoints;
SqsSettings sqsSettings = configSection.GetSection("AWS")
                                       .GetSection("SQS")
                                       .Get<SqsSettings>() ?? throw new Exception("AWS SQS configuration is missing");

builder.Services.TryAddFetchFundsCommand();
builder.AddRequestWithdrawFundsViaAtmRepository(dbName);
builder.AddRequestWithdrawFundsViaAtmSink(dbName, DateTimeOffset.UtcNow, kafkaSettings.Topic, kafkaEndpoints);

builder.Services.AddKafka2SQSHostedService(kafkaSettings, sqsSettings);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRequestWithdrawFundsViaATM();

await app.RunAsync();

