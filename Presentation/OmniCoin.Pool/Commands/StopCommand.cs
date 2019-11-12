


using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Pool.Commands
{
    internal static class StopCommand
    {
        internal static void Send(TcpState e, StopMsg stopMsg)
        {
            var stopCmd = PoolCommand.CreateCommand(CommandNames.Stop, stopMsg);
            if (PoolJob.TcpServer != null)
            {
                PoolJob.TcpServer.SendCommand(e, stopCmd);
            }
        }
    }
}
