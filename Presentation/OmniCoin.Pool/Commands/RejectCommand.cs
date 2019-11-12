


using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Pool.Commands
{
    internal static class RejectCommand
    {
        /// <summary>
        /// 发送Reject命令
        /// 创建命令，发送命令，关闭连接
        /// </summary>
        /// <param name="e"></param>
        internal static void Send(TcpState e)
        {
            var rejectCmd = PoolCommand.CreateCommand(CommandNames.Reject, null);
            if (PoolJob.TcpServer != null)
            {
                PoolJob.TcpServer.SendCommand(e, rejectCmd);
                PoolJob.TcpServer.CloseSocket(e);
            }
        }
    }
}
