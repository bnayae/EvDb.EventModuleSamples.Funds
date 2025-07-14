using EvDb.Core;
using Funds.RequestWithdrawFundsViaATM.Events;

namespace Funds.RequestWithdrawFundsViaATM.Repository;


[EvDbStreamFactory<IAtmFundsEvents, AtmFundsOutbox /* attach outbox */>(
                                        "funds:atm" /* stream type identifier */)]
public partial class AtmFundsFactory
{
    
}
