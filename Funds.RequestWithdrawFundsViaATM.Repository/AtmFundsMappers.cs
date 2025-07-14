using Funds.RequestWithdrawFundsViaATM.Events;
using Funds.RequestWithdrawFundsViaATM.Messages;
using Riok.Mapperly.Abstractions;

namespace Funds.RequestWithdrawFundsViaATM.Repository;

// ⚠ Don't forget the `partial` keyword
[Mapper]
public static partial class AtmFundsMappers
{
    // ⚠ Don't forget the `partial` keyword
    public static partial FundsWithdrawalRequestedViaAtmMessage ToMessage(this FundsFetchRequestedFromAtmEvent @event);
}


