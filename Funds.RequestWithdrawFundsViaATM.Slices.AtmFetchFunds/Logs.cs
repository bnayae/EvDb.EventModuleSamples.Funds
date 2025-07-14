using EvDb.Core;
using Microsoft.Extensions.Logging;

namespace Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;

internal static partial class Logs
{
    [LoggerMessage(LogLevel.Debug, "Fetch Funds From ATM Started {account}, {response}")]
    public static partial void LogFetchFundsFromAtm(this ILogger logger,
                                                           Guid account,
                                                           [LogProperties] StreamStoreAffected response);
}