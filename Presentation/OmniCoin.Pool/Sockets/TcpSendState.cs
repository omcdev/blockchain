


using OmniCoin.PoolMessages;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Pool.Sockets
{
    internal class TcpSendState : TcpState
    {
        internal PoolCommand Command { get; set; }
    }
}
