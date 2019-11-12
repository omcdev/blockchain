


using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;

namespace OmniCoin.Pool.Commands
{
    internal class StartCommand
    {
        internal static void Send(TcpState e, StartMsg startMsg)
        {
            var startCmd = PoolCommand.CreateCommand(CommandNames.Start, startMsg);
            if (PoolJob.TcpServer != null)
            {
                PoolJob.TcpServer.SendCommand(e, startCmd);
            }
        }

        internal static void Send(TcpState e)
        {
            var startMsg = new StartMsg();
            PoolTask poolCache = GetFreeMinerTask();

            var startCmd = PoolCommand.CreateCommand(CommandNames.Start, poolCache.CurrentStartMsg);
        }

        private static PoolTask GetFreeMinerTask()
        {
            PoolTask poolCache = new PoolTask();
            return poolCache;
        }
    }
}