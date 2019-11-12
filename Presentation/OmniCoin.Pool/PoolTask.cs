


using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.Pool.Models;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Pool
{
    internal class PoolTask
    {
        internal PoolTask()
        {
            MinerEfforts = new SafeCollection<MinerEffort>();
            SavingMinerEfforts = new SafeCollection<MinerEffort>();
        }

        internal string Id;

        internal BlockMsg GeneratingBlock;

        internal StartMsg CurrentStartMsg;

        internal long BaseTarget;

        internal MiningState State;

        internal long CurrentBlockHeight;

        internal int CurrentScoopNumber;

        internal long LastReceiveTime;

        internal long StartTime;

        internal SafeCollection<MinerEffort> MinerEfforts;

        internal SafeCollection<MinerEffort> SavingMinerEfforts;
    }

    internal enum MiningState
    {
        Wait,
        Mining
    }

}
