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
