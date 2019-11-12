


using OmniCoin.Framework;
using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Pool.Commands
{
    internal static class HeartbeatCommand
    {
        internal static void Receive(TcpReceiveState e, PoolCommand cmd)
        {
            UpdateHeartTime(e);
        }

        internal static void UpdateHeartTime(TcpState e)
        {
            var miner = PoolCache.WorkingMiners.FirstOrDefault(x => x.ClientAddress == e.Address);
            if (miner != null)
            {
                miner.LatestHeartbeatTime = Time.EpochTime;
            }
        }
    }
}
