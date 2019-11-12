


using OmniCoin.Consensus;
using OmniCoin.Framework;
using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Pool.Commands
{
    internal static class NonceDataCommand
    {
        internal static void Receive(TcpReceiveState e, PoolCommand cmd)
        {
            var msg = new NonceDataMsg();
            int index = 0;
            msg.Deserialize(cmd.Payload, ref index);

            var miner = PoolCache.WorkingMiners.FirstOrDefault(m => m.ClientAddress == e.Address);

            if (miner == null)
            {
                RejectCommand.Send(e);
                return;
            }

            var data = POC.CalculateScoopData(miner.WalletAddress, msg.MaxNonce, miner.CheckScoopNumber);

            if (Base16.Encode(data) == Base16.Encode(msg.ScoopData))
            {
                miner.IsConnected = true;
                miner.ConnectedTime = Time.EpochTime;
                miner.LatestHeartbeatTime = Time.EpochTime;
                LoginCommand.SendLoginResult(e, true);
                LogHelper.Info(miner.ClientAddress + " login success");
                
                StartCommand.Send(e);
            }
            else
            {
                LoginCommand.SendLoginResult(e, false);
                RejectCommand.Send(e);
                LogHelper.Info(miner.ClientAddress + " login fail");
            }
        }
    }
}