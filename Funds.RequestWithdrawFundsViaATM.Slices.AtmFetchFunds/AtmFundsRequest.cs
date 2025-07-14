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
