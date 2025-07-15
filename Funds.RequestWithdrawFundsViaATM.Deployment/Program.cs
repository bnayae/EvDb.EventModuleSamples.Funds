const string DATABASE_NAME = "tests";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.AddOtel();

var configSection = builder.Configuration.GetSection("RequestWithdrawFundsViaAtm");
string dbName = configSection.GetValue<string>("DatabaseName") ?? DATABASE_NAME;
string topiName = configSection.GetValue<string>("TopicName") ?? "atm.funds.withdraw";
string[] kafkaEndpoints = configSection.GetSection("Kafka")
                                        .GetValue<string[]>("Endpoint") ?? ["localhost:9092"];
builder.Services.TryAddFetchFundsCommand();
builder.AddRequestWithdrawFundsViaAtmRepository(dbName);
builder.AddRequestWithdrawFundsViaAtmSink(dbName, DateTimeOffset.UtcNow, topiName, kafkaEndpoints);

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
