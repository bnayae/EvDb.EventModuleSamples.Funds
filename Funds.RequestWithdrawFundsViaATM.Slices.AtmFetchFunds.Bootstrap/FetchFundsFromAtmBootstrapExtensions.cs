using Funds.Abstractions;
using Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class FetchFundsFromAtmBootstrapExtensions
{
    public static IEndpointRouteBuilder UseRequestWithdrawFundsViaATM(this IEndpointRouteBuilder app)
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
