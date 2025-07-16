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
- Add package reference `EvDb.Sinks.AwsAdmin`
- Add package reference `Confluent.Kafka`
- Add package reference `OpenTelemetry.Api`

- Add the following entry into the csproj file

  ```xml
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  ```

- Add `Settings` Folder
- Add the following types under the `Settings` folder:

```cs
namespace Kafka2SQS;

public record KafkaSettings
{
    public required string Topic { get; set; }
    public required string[] Endpoints { get; set; }
}
```

```cs
namespace Kafka2SQS;

public record SqsSettings
{
    public required string Endpoint { get; set; }
    public required string QueueName { get; set; }
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string Region { get; set; }
}
```

- Add the following types (under the project root):

```cs
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
```

```cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kafka2SQS;

internal static class Telemetry
{
    public const string TraceName = "Kafka2SQS";

    public static ActivitySource Trace { get; } = new ActivitySource(TraceName);
}

```

```cs
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


                using var activity = Telemetry.Trace.StartActivity("Kafka -> SQS", ActivityKind.Consumer);

                // Extract parent context from Kafka headers
                var parentContext = Propagator.Extract(default, consumeResult.Message.Headers, static (headers, key) =>
                {
                    var header = headers.FirstOrDefault(h => h.Key == key);
                    return header is null ? Enumerable.Empty<string>() : [Encoding.UTF8.GetString(header.GetValueBytes())];
                });
                var parentLink = new ActivityLink(parentContext.ActivityContext);
                activity?.AddLink(parentLink);

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
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogKafkaToSQSFailure(ex);
                consumer.Subscribe(_kafkaSettings.Topic); // re-listen to the topic
            }
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

        var request = new SendMessageRequest
        {
            QueueUrl = target,
            MessageBody = message,
            MessageAttributes = messageAttributes,
        };

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


```

```cs
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
```

## Create Flow Deployment

- Add ASP.NET Core Web API project `Funds.RequestWithdrawFundsViaATM.Deployment` under the `Request Withdraw Funds Via ATM` folder
  - 🗸 Enablecontainer support (Linux)
  - ✗ Use controllers
- Cleanup the weatherforecast sample
- Add project reference `Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds.Bootstrap`
- References
  - Add project reference `Funds.RequestWithdrawFundsViaATM.Repository.Bootstrap`
  - Add project reference `Kafka2SQS`
  - Add project reference `OpenTelemetry.Extensions.Hosting`
  - Add project reference `OpenTelemetry.Instrumentation.AspNetCore`
  - Add project reference `OpenTelemetry.Instrumentation.Http`
  - Add project reference `OpenTelemetry.Exporter.OpenTelemetryProtocol`
  - Add project reference `OpenTelemetry.Instrumentation.Runtime`
- Set up the `appsettings.Development.json`

  ```json
  {
    "Logging": {
      "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
      }
    },
    "RequestWithdrawFundsViaAtm": {
      "DatabaseName": "test",
      "Kafka": {
        "Topic": "atm.funds.withdraw",
        "Endpoints": ["localhost:9092"]
      },
      "AWS": {
        "SQS": {
          "Region": "us-east-1",
          "AccessKeyId": "test",
          "SecretAccessKey": "test",
          "Endpoint": "http://localhost:4566",
          "QueueName": "withdraw_approval"
        }
      }
    },
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
  ```

- Modify `Program.cs`

  ```cs
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
  ```

## Docker Compose

In order to run it on local machine set the following docker compose each under a dedicated folder.

```yml
name: evdb-sample-databases

volumes:
  mssql:
  psql:
  mongodb_data:

services:
  #  sqlserver:
  #    container_name: sqlserver-event-source-sample
  #    image: mcr.microsoft.com/mssql/server:2022-latest
  #    environment:
  #      SA_PASSWORD: "MasadNetunim12!@"
  #      ACCEPT_EULA: "Y"
  #    ports:
  #      - "1433:1433"
  #    # command: sh -c ' chmod +x /docker_init/entrypoint.sh; /docker_init/entrypoint.sh & /opt/mssql/bin/sqlservr;'
  #    restart: unless-stopped
  #
  #  psql:
  #    container_name: psql-event-source-sample
  #    image: postgres:latest
  #    environment:
  #      POSTGRES_USER: test_user
  #      POSTGRES_PASSWORD: MasadNetunim12!@
  #      POSTGRES_DB: test_db
  #    volumes:
  #      - psql:/var/lib/postgresql/data
  #      - ./dev/docker_psql_init:/docker-entrypoint-initdb.d
  #    ports:
  #      - 5432:5432
  #    restart: unless-stopped

  mongodb:
    image: mongo:8
    container_name: mongodb-event-source-sample
    volumes:
      - mongodb_data:/data/db
    environment:
      #  MONGO_INITDB_ROOT_USERNAME: rootuser
      #  MONGO_INITDB_ROOT_PASSWORD: MasadNetunim12!@
      MONGO_INITDB_DATABASE: evdb
    healthcheck:
      test: echo 'db.runCommand("ping").ok' | mongosh localhost:27017/test --quiet
      interval: 10s
      timeout: 10s
      retries: 5
      start_period: 40s
    ports:
      - "27017:27017"
    command: "--bind_ip_all --quiet --logpath /dev/null --replSet rs0"
    restart: unless-stopped

  mongo-init:
    image: mongo:8
    container_name: mongodb-event-source-sample-init
    depends_on:
      mongodb:
        condition: service_healthy
    command: >
      mongosh --host mongodb:27017 --eval
      '
      rs.initiate( {
         _id : "rs0",
         members: [
            { _id: 0, host: "localhost:27017" }
         ]
      })
      '
    restart: no
```

```yml
name: evdb-sample-kafka

services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    hostname: zookeeper
    container_name: zookeeper
    ports:
      - "2181:2181"
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    restart: unless-stopped # on-failure

  kafka:
    image: confluentinc/cp-kafka:7.5.0
    hostname: kafka
    container_name: kafka
    depends_on:
      - zookeeper
    ports:
      - "29092:29092"
      - "9092:9092"
      - "9101:9101"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: "zookeeper:2181"
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
      KAFKA_JMX_PORT: 9101
      KAFKA_JMX_HOSTNAME: localhost
    restart: unless-stopped # on-failure

  schema-registry:
    image: confluentinc/cp-schema-registry:7.5.0
    hostname: schema-registry
    container_name: schema-registry
    depends_on:
      - kafka
    ports:
      - "8081:8081"
    environment:
      SCHEMA_REGISTRY_HOST_NAME: schema-registry
      SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: "kafka:29092"
      SCHEMA_REGISTRY_LISTENERS: http://0.0.0.0:8081
    restart: unless-stopped # on-failure

  kafdrop:
    image: obsidiandynamics/kafdrop:3.30.0
    container_name: kafdrop
    depends_on:
      - kafka
    ports:
      - "9000:9000"
    environment:
      KAFKA_BROKERCONNECT: "kafka:29092"
      JVM_OPTS: "-Xms32M -Xmx64M"
    restart: unless-stopped

  kafka-ui:
    image: provectuslabs/kafka-ui:latest
    container_name: kafka-ui
    ports:
      - "8080:8080"
    depends_on:
      - kafka
    environment:
      - KAFKA_CLUSTERS_0_NAME=local
      - KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS=kafka:29092
      - KAFKA_CLUSTERS_0_ZOOKEEPER=zookeeper:2181
    restart: unless-stopped
```

```yml
name: evdb-sample-localstack

services:
  localstack:
    image: localstack/localstack:latest
    container_name: localstack
    ports:
      - "4566:4566" # Edge port
    environment:
      - SERVICES=sqs,sns
      - DEBUG=1
      - DOCKER_HOST=unix:///var/run/docker.sock
      - AWS_ACCESS_KEY_ID=test
      - AWS_SECRET_ACCESS_KEY=test
      - AWS_DEFAULT_REGION=us-east-1
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - localstack_data:/var/lib/localstack
    healthcheck:
      test: awslocal sns list-topics
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 10s
    restart: unless-stopped

  guistack:
    image: visualvincent/guistack:latest
    container_name: guistack
    ports:
      - "5000:80"
    environment:
      - AWS_SNS_ENDPOINT_URL=http://localstack:4566
      - AWS_SQS_ENDPOINT_URL=http://localstack:4566
      - AWS_REGION=us-east-1
    depends_on:
      - localstack
    restart: unless-stopped

volumes:
  localstack_data:
```

```yml
version: "3.7"
name: evdb-sample-otel
services:
  jaeger:
    image: jaegertracing/opentelemetry-all-in-one
    container_name: evdb-jaeger
    environment:
      - COLLECTOR_OTLP_ENABLED=true
    ports:
      - 16686:16686
      - 4318:4318
      - 4317:4317
    restart: unless-stopped
  aspire-dashboard:
    # https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard/configuration
    image: mcr.microsoft.com/dotnet/nightly/aspire-dashboard:latest
    container_name: evdb-aspire-dashboard
    ports:
      - 18888:18888
      - 18889:18889
      # - 4317:18889
    environment:
      - DASHBOARD__TELEMETRYLIMITS__MAXLOGCOUNT=1000
      - DASHBOARD__TELEMETRYLIMITS__MAXTRACECOUNT=1000
      - DASHBOARD__TELEMETRYLIMITS__MAXMETRICCOUNT=1000
      - DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true
    restart: unless-stopped
```
