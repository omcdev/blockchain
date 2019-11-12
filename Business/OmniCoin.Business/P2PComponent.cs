

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.DataAgent;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OmniCoin.Data;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Data.Dacs;

namespace OmniCoin.Business
{
    public class P2PComponent
    {
        public void P2PStart(Guid guid, string ip, int port, bool isTracker)
        {
            P2P.Instance.Start(guid, ip,port, isTracker);
            P2P.Instance.BlackList = this.GetAllBlackListItemAddresses();
        }

        public void P2PStop()
        {
            P2P.Instance.Stop();
        }

        public void SetBlockHeightAndTime(long blockHeight, long blockTime)
        {
            P2P.Instance.LastBlockHeight = blockHeight;
            P2P.Instance.LastBlockTime = blockTime;
        }

        public bool IsRunning()
        {
            return P2P.Instance.IsRunning;
        }
        public bool GetNetTotals(out long totalBytesSent, out long totalBytesReceived)
        {
            if(P2P.Instance.IsRunning)
            {
                totalBytesReceived = P2P.Instance.TotalBytesReceived;
                totalBytesSent = P2P.Instance.TotalBytesSent;
                return true;
            }
            else
            {
                totalBytesReceived = 0;
                totalBytesSent = 0;
                return false;
            }
        }

        public void AddNode(string address, int port)
        {
            P2P.Instance.ConnectToNewPeer(address, port);
        }

        public bool RemoveNode(string address, int port)
        {
            return P2P.Instance.RemovePeer(address, port);
        }

        public List<P2PNode> GetNodes()
        {
            return P2P.Instance.Peers.ToList();
        }

        public P2PNode GetNodeByAddress(string address, int port)
        {
            var peer = P2P.Instance.Peers.Where(p => p.IP == address && p.Port == port).FirstOrDefault();
            return peer;
        }

        public void SendCommand(string address, int port, P2PCommand command)
        {
            P2P.Instance.Send(command, address, port);
        }

        public void RegisterMessageReceivedCallback(Action<P2PState> action)
        {
            P2P.Instance.DataReceived = action;
        }

        public void RegisterNodeConnectedStateChangedCallback(Action<P2PNode> action)
        {
            P2P.Instance.NodeConnectionStateChanged = action;
        }

        public void AddIntoBlackList(string address, int port, long? expiredTime)
        {
            BlackPeerDac.Default.Save(address + ":" + port, expiredTime);

            if(P2P.Instance.IsRunning)
            {
                P2P.Instance.BlackList = this.GetAllBlackListItemAddresses();
            }
        }

        public void RemoveFromBlackList(string address, int port)
        {
            BlackPeerDac.Default.Delete(address + ":" + port);

            if (P2P.Instance.IsRunning)
            {
                P2P.Instance.BlackList.Remove(address + ":" + port);
            }
        }

        public List<BlackListItem> GetBlackList()
        {
            return BlackPeerDac.Default.GetAll();
        }

        public void ClearBlackList()
        {
            BlackPeerDac.Default.DeleteAll();

            if (P2P.Instance.IsRunning)
            {
                P2P.Instance.BlackList.Clear();
            }
        }

        public bool ExistedInBlackList(string address, string port)
        {
            return BlackPeerDac.Default.CheckExists(address + ":" + port);
        }

        public List<string> GetAllBlackListItemAddresses()
        {
            var items = BlackPeerDac.Default.GetAll();
            var result = new List<string>();

            foreach (var item in items)
            {
                if (!item.Expired.HasValue || item.Expired.Value < Time.EpochTime)
                {
                    result.Add(item.Address);
                }
            }

            return result;
        }
    }
}
