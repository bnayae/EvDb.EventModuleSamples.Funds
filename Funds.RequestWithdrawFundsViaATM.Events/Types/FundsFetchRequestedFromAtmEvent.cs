using EvDb.Core;
using Funds.Abstractions;

namespace Funds.RequestWithdrawFundsViaATM.Events;

/// <summary>
/// Funds fetch requested event via ATM
/// </summary>
[EvDbDefineEventPayload("funds-fetch-requested-from-ATM")]
public readonly partial record struct FundsFetchRequestedFromAtmEvent(AccountId AccountId)
{

    /// <summary>
    /// Id of financial operation like moving money from one account to another (can related to multiple transactons)
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
