using EvDb.Core;

namespace Funds.RequestWithdrawFundsViaATM.Events;

[EvDbAttachEventType<FundsFetchRequestedFromAtmEvent>]
[EvDbAttachEventType<FundsFetchRequestedFromAtmDeniedEvent>]
public partial interface IAtmFundsEvents
{
}
