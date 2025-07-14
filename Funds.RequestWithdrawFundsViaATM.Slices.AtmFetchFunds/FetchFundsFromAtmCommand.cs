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

    async Task ICommandHandler<AtmFundsRequest>.ExecuteAsync(AtmFundsRequest request,
                                        CancellationToken cancellationToken)
    {
        AccountId accountId = request.AccountId;
        IEvDbAtmFunds stream = await _fundsFactory.GetAsync(accountId, cancellationToken);

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
