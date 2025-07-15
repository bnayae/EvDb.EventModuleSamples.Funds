using Funds.Abstractions;
using Funds.RequestWithdrawFundsViaATM.Slices.AtmFetchFunds;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class FetchFundsSliceExtensions
{
    public static IServiceCollection TryAddFetchFundsCommand(this IServiceCollection services)
    {
        services.TryAddSingleton<ICommandHandler<AtmFundsRequest>, FetchFundsFromAtmCommand>();
        return services;
    }
}