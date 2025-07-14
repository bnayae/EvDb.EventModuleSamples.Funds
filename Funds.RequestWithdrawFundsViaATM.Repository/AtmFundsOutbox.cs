using EvDb.Core;
using Funds.RequestWithdrawFundsViaATM.Events;
using Funds.RequestWithdrawFundsViaATM.Messages;

namespace Funds.RequestWithdrawFundsViaATM.Repository;

[EvDbAttachMessageType<FundsWithdrawalRequestedViaAtmMessage>] // declare what messages can be produced by this outbox
[EvDbOutbox<AtmFundsFactory>] // declare the factory that will be used to create the outbox
public partial class AtmFundsOutbox
{
    protected override void ProduceOutboxMessages(FundsFetchRequestedFromAtmEvent payload,
                                                  IEvDbEventMeta meta,
                                                  EvDbAtmFundsViews views,
                                                  AtmFundsOutboxContext outbox)
    {
        var message = payload.ToMessage();
        outbox.Append(message);
    }
}


