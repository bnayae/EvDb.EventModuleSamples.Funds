using Funds.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

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
