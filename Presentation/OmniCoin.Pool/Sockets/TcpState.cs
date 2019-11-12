


using System.Net.Sockets;

namespace OmniCoin.Pool.Sockets
{
    internal class TcpState
    {
        internal TcpClient Client { get; set; }
        internal NetworkStream Stream { get; set; }
        internal string Address { get; set; }
    }
}
