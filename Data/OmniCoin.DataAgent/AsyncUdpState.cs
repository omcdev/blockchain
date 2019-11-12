// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Messages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FiiiChain.DataAgent
{
    public class AsyncUdpState
    {
        //// Client   socket.  
        //public UdpClient udpClient = null;
        //// Size of receive buffer.  
        //public const int BufferSize = 1024;
        //// Receive buffer.  
        //public byte[] buffer = new byte[BufferSize];
        //// Received data string.  
        ////public StringBuilder sb = new StringBuilder();

        //public IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

        public string IP { get; set; }
        public int Port { get; set; }
        public P2PCommand Command { get; set; }
    }
}
