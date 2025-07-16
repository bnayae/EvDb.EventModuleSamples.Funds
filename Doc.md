# From Event Modeling to Event Sourcing

- Create Blank Solution
- Add `Directory.Build.props`

  ```xml
    <Project>
    <PropertyGroup>
      <Version>1.0.0</Version>
    </PropertyGroup>

    <PropertyGroup>
      <TargetFramework>net8.0</TargetFramework>
      <LangVersion>13.0</LangVersion>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <PropertyGroup>
      <EnableNETAnalyzers>true</EnableNETAnalyzers>
      <AnalysisMode>AllEnabledByDefault</AnalysisMode>
      <AnalysisLevel>6.0-all</AnalysisLevel>
    </PropertyGroup>

    <PropertyGroup>
      <RepositoryType>git</RepositoryType>
      <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
      <GenerateBindingRedirectsOutputType>true</GenerateBindingRedirectsOutputType>
    </PropertyGroup>

    <PropertyGroup>
      <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
      <PublishRepositoryUrl>true</PublishRepositoryUrl>
      <IncludeSource>True</IncludeSource>
      <IncludeSymbols>True</IncludeSymbols>
      <SymbolPackageFormat>snupkg</SymbolPackageFormat>
      <GenerateDocumentationFile>True</GenerateDocumentationFile>
    </PropertyGroup>

    <PropertyGroup>
      <GenerateDocumentationFile>true</GenerateDocumentationFile>
      <NoWarn>$(NoWarn);1591</NoWarn>
    </PropertyGroup>

  </Project>
  ```

## Set up domain level abstractions

- Add a class library project `Funds.Abstractions`
- Add package reference `Vogen` (use to create `Contextual Type`)
- Add `Contextual Type` folder _(wraps a primitive to add semantic meaning and enforce domain constraints within a bounded context)_
- Add the following Types:

  ```cs
  using Microsoft.VisualBasic;
  using Vogen;

  namespace Funds.Abstractions;

  /// <summary>
  /// The account identifier
  /// </summary>
  [ValueObject<Guid>(Conversions.TypeConverter | Conversions.SystemTextJson,
      toPrimitiveCasting: CastOperator.Implicit,
      fromPrimitiveCasting: CastOperator.Implicit,
      tryFromGeneration: TryFromGeneration.GenerateBoolMethod,
      isInitializedMethodGeneration: IsInitializedMethodGeneration.Generate)]
  public readonly partial struct AccountId
  {
  }
  ```

  ```cs
  using Microsoft.VisualBasic;
  using System.Globalization;
  using Vogen;

  namespace Funds.Abstractions;

  /// <summary>
  /// The currency ISO 3 letter code
  /// </summary>
  [ValueObject<string>(Conversions.TypeConverter | Conversions.SystemTextJson,
      toPrimitiveCasting: CastOperator.Implicit,
      fromPrimitiveCasting: CastOperator.Implicit,
      tryFromGeneration: TryFromGeneration.GenerateBoolMethod,
      isInitializedMethodGeneration: IsInitializedMethodGeneration.Generate)]
  public readonly partial struct Currency
  {
      private static Validation Validate(string input) => input.Length == 3
                          ? Validation.Ok
                          : Validation.Invalid("Expecting 3 Letters ISO standard");

      private static string NormalizeInput(string input) => input.Trim().ToUpper  (CultureInfo.InvariantCulture);

  }
  ```

  ```cs
  using Microsoft.VisualBasic;
  using Vogen;

  namespace Funds.Abstractions;

  /// <summary>
  /// The account identifier
  /// </summary>
  [ValueObject<Guid>(Conversions.TypeConverter | Conversions.SystemTextJson,
      toPrimitiveCasting: CastOperator.Implicit,
      fromPrimitiveCasting: CastOperator.Implicit,
      tryFromGeneration: TryFromGeneration.GenerateBoolMethod,
      isInitializedMethodGeneration: IsInitializedMethodGeneration.Generate)]
  public readonly partial struct PaymentId
  {
  }
  ```

  ```cs
  using Microsoft.VisualBasic;
  using Vogen;

  namespace Funds.Abstractions;

  /// <summary>
  /// The account identifier
  /// </summary>
  [ValueObject<Guid>(Conversions.TypeConverter | Conversions.SystemTextJson,
      toPrimitiveCasting: CastOperator.Implicit,
      fromPrimitiveCasting: CastOperator.Implicit,
      tryFromGeneration: TryFromGeneration.GenerateBoolMethod,
      isInitializedMethodGeneration: IsInitializedMethodGeneration.Generate)]
  public readonly partial struct TransactionId
  {
  }
  ```

- Add `Interfaces` folder (for common contracts and conventions)
- Add the following interfaces:

  ```cs
  namespace Funds.Abstractions;

  /// <summary>
  /// Command contract
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public interface ICommandHandler<in T>
  {
      /// <summary>
      /// Execute the command.
      /// </summary>
      /// <param name="request">The request.</param>
      /// <param name="cancellationToken">The cancellation token.</param>
      /// <returns></returns>
      Task ExecuteAsync(T request,
                        CancellationToken cancellationToken = default);
  }
  ```

## Request Withdraw Funds Via ATM (flow)

Create the following solution folder structure

- `Request Withdraw Funds Via ATM`
- `Events`: represent the private data entities (state change records / facts)
- `Messages`: represent public transient data (cross-service change notification)
- `Slices`: vertical logic isolation
  - `ATM Fetch Funds`
- `Swimlanes`: repository operation (event/messages ORM)
  - `ATM Funds`

## Events

- Add a class library project `Funds.RequestWithdrawFundsViaATM.Events`
- Add project reference `Funds.Abstractions`
- Add package reference `EvDb.Abstractions`
- Add folder `Types`

  - Add the following events types

    _⚠ Don't forget the `partial` keyword._

    ```cs
    using EvDb.Core;
    using Funds.Abstractions;

    namespace Funds.RequestWithdrawFundsViaATM.Events;

    /// <summary>
    /// Funds fetch requested event via ATM
    /// </summary>
    [EvDbDefineEventPayload("funds-fetch-requested-from-ATM")]
    public readonly partial record struct FundsFetchRequestedFromAtmEvent(AccountId     AccountId)
    {

        /// <summary>
        /// Id of financial operation like moving money from one account to another     (can related to multiple transactons)
        /// </summary>
        public required PaymentId PaymentId { get; init; }
        /// <summary>
        /// Id of a financial execution unit like withdrawal or deposit
        /// </summary>
        public required TransactionId TransactionId { get; init; }
        /// <summary>
        /// Date of a financial execution unit like withdrawal or deposit
        /// </summary>
        public required DateTimeOffset TransactionDate { get; init; }
        /// <summary>
        /// The currency of the transaction
        /// </summary>
        public required Currency Currency { get; init; }
        /// <summary>
        /// The money amount
        /// </summary>
        public required double Amount { get; init; }
    }
    ```

    ```cs
    using EvDb.Core;
    using Funds.Abstractions;

    namespace Funds.RequestWithdrawFundsViaATM.Events;

    /// <summary>
    /// Funds fetch requested event denied via ATM
    /// </summary>
    [EvDbDefineEventPayload("funds-fetch-requested-from-ATM-denied")]
    public readonly partial record struct FundsFetchRequestedFromAtmDeniedEvent    (AccountId AccountId)
    {

        /// <summary>
        /// Id of financial operation like moving money from one account to another     (can related to multiple transactons)
        /// </summary>
        public required PaymentId PaymentId { get; init; }
        /// <summary>
        /// Id of a financial execution unit like withdrawal or deposit
        /// </summary>
        public required TransactionId TransactionId { get; init; }
        /// <summary>
        /// Date of a financial execution unit like withdrawal or deposit
        /// </summary>
        public required DateTimeOffset TransactionDate { get; init; }
        /// <summary>
        /// The currency of the transaction
        /// </summary>
        public required Currency Currency { get; init; }
        /// <summary>
        /// The money amount
        /// </summary>
        public required double Amount { get; init; }
    }
    ```

EvDb uses the value provided in the attribute parameter as the **event type identifier** in storage.  
It intentionally avoids relying on the class or record name, as those can change during refactoring, while the event type should remain stable over time.

> ✅ **Best Practice**: Use `readonly record struct` for events.  
> This ensures immutability and is more GC-friendly.

However, you're free to use other types such as:

- `readonly record struct`
- `record`
- `class`
- `struct`

> 💡 The syntax style is flexible—use traditional syntax or primary constructors based on your preference.  
> What matters is the structure and stability of the event type, not the syntax used to define it.

- Add `IAtmFundsEvents.cs`: An event bundle that encapsulates a group of related events, later consumed by other parts of the system such as streams, aggregate (view), or outbox mechanisms.

  _⚠ Don't forget the `partial` keyword._

  ```cs
  using EvDb.Core;

  namespace Funds.RequestWithdrawFundsViaATM.Events;

  [EvDbAttachEventType<FundsFetchRequestedFromAtmEvent>]
  [EvDbAttachEventType<FundsFetchRequestedFromAtmDeniedEvent>]
  public partial interface IAtmFundsEvents
  {
  }
  ```

  - To verify it's set up correctly, compile and use **Go to Definition (F12)**. You’ll see two class files: the one you wrote and a generated one. This is why the `partial` keyword matters. EvDb removes boilerplate and ensures everything stays coherent and follows the standard.

## Messages

- Add class library project `Funds.RequestWithdrawFundsViaATM.Messages`
- Add project reference `Funds.Abstractions`
- Add package reference `EvDb.Abstractions`
- Add the following message:  
  _⚠ Don't forget the `partial` keyword._

  ```cs
  using EvDb.Core;
  using Funds.Abstractions;

  namespace Funds.RequestWithdrawFundsViaATM.Messages;


  /// <summary>
  /// Withdrawal requested message via ATM
  /// </summary>
  /// <param name="AccountId">Account identifier</param>
  [EvDbDefineMessagePayload("funds-deposited-requested-via-atm")]
  public readonly partial record struct FundsWithdrawalRequestedViaAtmMessage  (AccountId AccountId)
  {

      /// <summary>
      /// Id of financial operation like moving money from one account to another   (can related to multiple transactons)
      /// </summary>
      public required PaymentId PaymentId { get; init; }
      /// <summary>
      /// Id of a financial execution unit like withdrawal or deposit
      /// </summary>
      public required TransactionId TransactionId { get; init; }
      /// <summary>
      /// Date of a financial execution unit like withdrawal or deposit
      /// </summary>
      public required DateTimeOffset TransactionDate { get; init; }
      /// <summary>
      /// The currency of the transaction
      /// </summary>
      public required Currency Currency { get; init; }
      /// <summary>
      /// The money amount
      /// </summary>
      public required double Amount { get; init; }
  }
  ```

## ATM Funds (Swimlane)

### Create stream repository for storing the event.

- Add class library project `Funds.RequestWithdrawFundsViaATM.Repository`
- Add project reference `Funds.RequestWithdrawFundsViaATM.Events`
- Add project reference `Funds.RequestWithdrawFundsViaATM.Messages`
- Add package reference `EvDb.Core`
- Add package reference `Riok.Mapperly`
- Add a stream factory:  
  _⚠ Don't forget the `partial` keyword._

  ```cs
  using EvDb.Core;
  using Funds.RequestWithdrawFundsViaATM.Events;
  using Funds.RequestWithdrawFundsViaATM.Messages;

  namespace Funds.RequestWithdrawFundsViaATM.Repository;

  [EvDbStreamFactory<IAtmFundsEvents>("funds:atm" /* stream type identifier */)]
  public partial class AtmFundsFactory
  {

  }
  ```

## Attach outbox logic to the stream factory

- Define a mapper (event-> message)

  ```cs
  using Funds.RequestWithdrawFundsViaATM.Events;
  using Funds.RequestWithdrawFundsViaATM.Messages;
  using Riok.Mapperly.Abstractions;

  namespace Funds.RequestWithdrawFundsViaATM.Repository;

  // ⚠ Don't forget the `partial` keyword
  [Mapper]
  public static partial class AtmFundsMappers
  {
      // ⚠ Don't forget the `partial` keyword
      public static partial FundsWithdrawalRequestedViaAtmMessage ToMessage(this   FundsFetchRequestedFromAtmEvent @event);
  }
  ```

- Create the outbox's logic class

  ```cs
  using EvDb.Core;
  using Funds.RequestWithdrawFundsViaATM.Events;
  using Funds.RequestWithdrawFundsViaATM.Messages;

  namespace Funds.RequestWithdrawFundsViaATM.Repository;

  [EvDbAttachMessageType<FundsWithdrawalRequestedViaAtmMessage>] // declare what   messages can be produced by this outbox
  [EvDbOutbox<AtmFundsFactory>] // declare the factory that will be used to create   the outbox
  public partial class AtmFundsOutbox
  {
      protected override void ProduceOutboxMessages(
                                 FundsFetchRequestedFromAtmEvent payload,
                                 IEvDbEventMeta meta,
                                 EvDbAtmFundsViews views,
                                 AtmFundsOutboxContext outbox)
      {
          var message = payload.ToMessage();
          outbox.Append(message);
      }
  }
  ```

- Add the outbox to the stream factory (edit the EvDbStreamFactory attribute)

  ```cs
  // With the outbox
  [EvDbStreamFactory<IAtmFundsEvents, AtmFundsOutbox >("funds:atm")]
  public partial class AtmFundsFactory
  {
  }
  ```

## Add swimlane bootstrap

- Add class library project `Funds.RequestWithdrawFundsViaATM.Repository.Bootstrap` under `ATM Funds` swimlane's folder
- Add project reference `Funds.RequestWithdrawFundsViaATM.Repository`
- Add package reference `EvDb.Adapters.Store.EvDbMongoDB`
- Add package reference `EvDb.Sinks.EvDbSinkKafka`
- Add the following entry into the csproj file

  ```xml
  <ItemGroup>
  	<FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  ```

- Add the following type:

  ```cs
  using Confluent.Kafka;
  using EvDb.Core;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Hosting;

  namespace namespace Microsoft.Extensions.DependencyInjection;

  public static class RequestWithdrawFundsViaAtmRepositoryExtensions
  {
      private const string DEFAULT_STREAM_PREFIX = "ATM.Funds";

      public static EvDbStorageContext CreateContext(
                                                  this IHostApplicationBuilder builder,
                                                  string databaseName)
      {
          EvDbStorageContext context = new EvDbStorageContext(databaseName, builder.  Environment.EnvironmentName, DEFAULT_STREAM_PREFIX);
          return context;
      }

      public static IHostApplicationBuilder AddRequestWithdrawFundsViaArmRepository(
                                                  this IHostApplicationBuilder builder,
                                                  string databaseName)
      {
          IServiceCollection services = builder.Services;
          EvDbStorageContext context = builder.CreateContext(databaseName);
          services.AddEvDb()
                  .AddAtmFundsFactory(storage => storage.UseMongoDBStoreForEvDbStream(),   context)
                  .DefaultSnapshotConfiguration(storage => storage.  UseMongoDBForEvDbSnapshot());

          return builder;
      }

      public static IHostApplicationBuilder AddRequestWithdrawFundsViaAtmRepository(this   IHostApplicationBuilder builder,
                                                                               string   databaseName  ,
                                                                               DateTimeOffs  et since,
                                                                               string   topicName,
                                                                               string   kafkaEndpoin  t =   "localhost:9  092")
      {
          ProducerConfig config = new ()
          {
              BootstrapServers = kafkaEndpoint
          };

          return builder.AddRequestWithdrawFundsViaAtmRepository(databaseName, config,   since, topicName);
      }

      public static IHostApplicationBuilder AddRequestWithdrawFundsViaAtmRepository(this   IHostApplicationBuilder builder,
                                                                               string   databaseName  ,
                                                                               ProducerConf  ig config,
                                                                               DateTimeOffs  et since,
                                                                               string   topicName)
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
  ```

## ATM Fetch Funds (Slice)

- Add class library project `Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds` under `ATM Fetch Funds` folder
- Add project reference `Funds.RequestWithdrawFundsViaATM.Repository`
- Add [high perf logging](https://andrewlock.net/exploring-dotnet-6-part-8-improving-logging-performance-with-source-generators/#the-net-6-loggermessage-source-generator) class

  ```cs
  using EvDb.Core;
  using Microsoft.Extensions.Logging;

  namespace Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;

  internal static partial class Logs
  {
      [LoggerMessage(LogLevel.Debug, "Fetch Funds From ATM Started {account},   {response}")]
      public static partial void LogFetchFundsFromAtm(this ILogger logger,
                                                             Guid account,
                                                             [LogProperties]   StreamStoreAffected   response);
  }
  ```

- Add request class

  ```cs
  using Funds.Abstractions;

  namespace Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;

  public readonly partial record struct AtmFundsRequest(AccountId AccountId)
  {
      /// <summary>
      /// The currency of the transaction
      /// </summary>
      public required Currency Currency { get; init; }
      /// <summary>
      /// The money amount
      /// </summary>
      public required double Amount { get; init; }
  }
  ```

- Add a command

  ```cs
  using EvDb.Core;
  using Funds.Abstractions;
  using Funds.RequestWithdrawFundsViaATM.Events;
  using Funds.RequestWithdrawFundsViaATM.Repository;
  using Microsoft.Extensions.Logging;

  namespace Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;

  internal sealed class FetchFundsFromAtmCommand : ICommandHandler<AtmFundsRequest>
  {
      private readonly ILogger<FetchFundsFromAtmCommand> _logger;
      private readonly IEvDbAtmFundsFactory _fundsFactory;

      public FetchFundsFromAtmCommand(
          ILogger<FetchFundsFromAtmCommand> logger,
          IEvDbAtmFundsFactory fundsFactory)
      {
          _logger = logger;
          _fundsFactory = fundsFactory;
      }

      async Task ICommandHandler<AtmFundsRequest>.ExecuteAsync(AtmFundsRequest   request,
                                          CancellationToken cancellationToken)
      {
          AccountId accountId = request.AccountId;
          IEvDbAtmFunds stream = await _fundsFactory.GetAsync(accountId,   cancellationToken);

          if (request.Amount < 1000)
          {
              var e = new FundsFetchRequestedFromAtmEvent(accountId)
              {
                  Amount = request.Amount,
                  Currency = request.Currency,
                  TransactionDate = DateTimeOffset.UtcNow,
                  TransactionId = Guid.NewGuid(),
                  PaymentId = Guid.NewGuid(),
              };
              await stream.AppendAsync(e);
          }
          else
          {
              var e = new FundsFetchRequestedFromAtmDeniedEvent(accountId)
              {
                  Amount = request.Amount,
                  Currency = request.Currency,
                  TransactionDate = DateTimeOffset.UtcNow,
                  TransactionId = Guid.NewGuid(),
                  PaymentId = Guid.NewGuid(),
              };
              await stream.AppendAsync(e);
          }

          StreamStoreAffected response = await stream.StoreAsync(cancellationToken);
          _logger.LogFetchFundsFromAtm(accountId, response);
      }
  }
  ```

- Add Dependency Injection registration

  ```cs
  using Funds.Abstractions;
  using Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.DependencyInjection.Extensions;

  namespace namespace Microsoft.Extensions.DependencyInjection;

  public static class FetchFundsSliceExtensions
  {
      public static IServiceCollection TryAddFetchFundsCommand(this IServiceCollection services)
      {
          services.TryAddSingleton<ICommandHandler<AtmFundsRequest>,  FetchFundsFromAtmCommand>();
          return services;
      }
  }
  ```

  ### Add a bootstrap project

  The bootstap is responsible for the slice registration (into Dependency Injection).

- Add class library project `Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds.Bootstrap`
- Add project reference `Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds`
- Add the following entry into the csproj file

  ```xml
  <ItemGroup>
  	<FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  ```

- Add request type

  ```cs
  using Funds.Abstractions;

  namespace namespace Microsoft.Extensions.DependencyInjection;

  public readonly partial record struct AtmFundsApiRequest
  {
      /// <summary>
      /// The currency of the transaction
      /// </summary>
      public required Currency Currency { get; init; }
      /// <summary>
      /// The money amount
      /// </summary>
      public required double Amount { get; init; }
  }
  ```

- Add Dependency Injection extensions

  ```cs
  using Funds.Abstractions;
  using Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;
  using Microsoft.AspNetCore.Builder;
  using Microsoft.AspNetCore.Http;
  using Microsoft.AspNetCore.Routing;
  using Microsoft.Extensions.DependencyInjection;

  namespace namespace Microsoft.Extensions.DependencyInjection;

  public static class FetchFundsFromAtmBootstrapExtensions
  {
      public static IServiceCollection AddRequestWithdrawFundsViaATM(this   IServiceCollection services)
      {
          // register the command
          services.TryAddFetchFundsCommand();
          return services;
      }

      public static IEndpointRouteBuilder UseRequestWithdrawFundsViaATM(this   IEndpointRouteBuilder app)
      {
          // setup the endpoint
          var withdraw = app.MapGroup("withdraw")
           .WithTags("Withdraw");

          withdraw.MapPost("ATM/{account}",
              async (AccountId account,
              AtmFundsApiRequest data,
              ICommandHandler<AtmFundsRequest> slice) =>
              {
                  AtmFundsRequest request = new AtmFundsRequest(account)
                  {
                      Currency = data.Currency,
                      Amount = data.Amount
                  };
                  await slice.ExecuteAsync(request);
                  return Results.Ok();
              });
          return app;
      }

  }
  ```

## Kafka to SQS

Build an infrastructue to move kafka messages into SQS (stream into commands).

- Add solution folder `Infrastructure` at the solution root.
- Add class library project `Kafka2SQS` within the `Infrastructure` folder
- Add package reference `AWSSDK.SQS`
- Add package reference `Confluent.Kafka`

- Add the following entry into the csproj file

  ```xml
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  ```

- Add the following types:

```cs

```

## Create Flow Deployment

- Add ASP.NET Core Web API project `Funds.RequestWithdrawFundsViaATM.Deployment` under the `Request Withdraw Funds Via ATM` folder
  - 🗸 Enablecontainer support (Linux)
  - ✗ Use controllers
- Cleanup the weatherforecast sample
- Add project reference `Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds.Bootstrap`
- Add project reference `Funds.RequestWithdrawFundsViaATM.Repository.Bootstrap`
- Add project reference `OpenTelemetry.Extensions.Hosting`
- Add project reference `OpenTelemetry.Instrumentation.AspNetCore`
- Add project reference `OpenTelemetry.Instrumentation.Http`
- Add project reference `OpenTelemetry.Exporter.OpenTelemetryProtocol`
- Add project reference `OpenTelemetry.Instrumentation.Runtime`
- Add connection to the `appsetting.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "EvDbMongoDBConnection": "mongodb://localhost:27017"
  }
}
```

- Add Open Telemetry (Otel) extention

  ```cs
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
                     .AddOtlpExporter("aspire", o => o.Endpoint = new Uri($"{otelHost}  :18889"));
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
                                                      autoGenerateServiceInstanceId:   false)) // builder.Environment.  ApplicationName
              .WithTracing(tracing =>
              {
                  tracing
                          .AddEvDbInstrumentation()
                          .AddEvDbStoreInstrumentation()
                          .AddEvDbSinkKafkaInstrumentation()
                          .AddSource("MongoDB.Driver.Core.Extensions.DiagnosticSources")
                          .SetSampler<AlwaysOnSampler>()
                          .AddOtlpExporter()
                          .AddOtlpExporter("aspire", o => o.Endpoint = new Uri($"{otelHost}  :18889"));
              })
              .WithMetrics(meterBuilder =>
                      meterBuilder.AddEvDbInstrumentation()
                                  .AddEvDbStoreInstrumentation()
                                  .AddEvDbSinkKafkaInstrumentation()
                                  .AddMeter("MongoDB.Driver.Core.Extensions.  DiagnosticSources")
                                  .AddOtlpExporter()
                                  .AddOtlpExporter("aspire", o => o.Endpoint = new Uri($"  {otelHost}:18889")));

          return builder;
      }

  }
  ```

- Modify `Program.cs`

  ```cs
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
  builder.AddRequestWithdrawFundsViaAtmSink(dbName, DateTimeOffset.UtcNow, topiName,   kafkaEndpoints);

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
  ```
