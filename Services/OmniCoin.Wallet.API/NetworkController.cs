


using EdjCase.JsonRpc.Router;
using EdjCase.JsonRpc.Router.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmniCoin.DTO;
using OmniCoin.Framework;
using OmniCoin.Business;
using OmniCoin.Messages;
using OmniCoin.Consensus;
using OmniCoin.Entities;

namespace OmniCoin.Wallet.API
{
    public class NetworkController : BaseRpcController
    {
        public IRpcMethodResult GetNetTotals()
        {
            try
            {
                var p2pComponent = new P2PComponent();
                if(!p2pComponent.IsRunning())
                {
                    throw new CommonException(ErrorCode.Service.Network.P2P_SERVICE_NOT_START);
                }

                var result = new GetNetTotalsOM();
                long totalRecv, totalSent;

                if(p2pComponent.GetNetTotals(out totalSent, out totalRecv))
                {
                    result.timeMillis = Time.EpochTime;
                    result.totalBytesRecv = totalRecv;
                    result.totalBytesSent = totalSent;
                    return Ok(result);
                }
                else
                {
                    throw new CommonException(ErrorCode.Service.Network.P2P_SERVICE_NOT_START);
                }
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult GetNetworkInfo()
        {
            try
            {
                var result = new GetNetworkInfoOM();
                var p2pComponent = new P2PComponent();
                result.version = Versions.EngineVersion;
                result.protocolVersion = Versions.MsgVersion;
                result.minimumSupportedVersion = Versions.MinimumSupportVersion;
                result.isRunning = p2pComponent.IsRunning();

                var nodes = p2pComponent.GetNodes();
                result.connections = nodes.Where(n => n.IsConnected).Count();

                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult GetPeerInfo()
        {
            try
            {
                var p2pComponent = new P2PComponent();
                if (!p2pComponent.IsRunning())
                {
                    throw new CommonException(ErrorCode.Service.Network.P2P_SERVICE_NOT_START);
                }

                var result = new List<GetPeerInfoOM>();
                var nodes = p2pComponent.GetNodes();

                for(int i = 0; i < nodes.Count; i ++)
                {
                    var node = nodes[i];

                    if(node.IsConnected)
                    {
                        var info = new GetPeerInfoOM();
                        info.id = result.Count;
                        info.isTracker = node.IsTrackerServer;
                        info.addr = node.IP + ":" + node.Port;
                        info.lastSend = node.LastSentTime;
                        info.lastRecv = node.LastReceivedTime;
                        info.lastHeartBeat = node.LastHeartbeat;
                        info.bytesSent = node.TotalBytesSent;
                        info.bytesRecv = node.TotalBytesReceived;
                        info.connTime = node.ConnectedTime;
                        info.version = node.Version;
                        info.inbound = node.IsInbound;
                        info.latestHeight = node.LatestHeight;

                        result.Add(info);
                    }
                }

                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult GetConnectionCount()
        {
            try
            {
                var p2pComponent = new P2PComponent();
                var nodes = p2pComponent.GetNodes();
                var result = nodes.Where(n => n.IsConnected).Count();

                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult AddNode(string nodeAddress)
            {
            try
            {
                var p2pComponent = new P2PComponent();
                if (!p2pComponent.IsRunning())
                {
                    throw new CommonException(ErrorCode.Service.Network.P2P_SERVICE_NOT_START);
                }

                var texts = nodeAddress.Split(':');
                int port = 0;

                if(texts.Length != 2 || 
                    !int.TryParse(texts[1], out port) || 
                    port <= 0 || 
                    port >= 65536)
                {
                    throw new CommonException(ErrorCode.Service.Network.NODE_ADDRESS_FORMAT_INVALID);
                }

                p2pComponent.AddNode(texts[0], port);
                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult GetAddedNodeInfo(string nodeAddress)
        {
            try
            {
                var p2pComponent = new P2PComponent();
                if (!p2pComponent.IsRunning())
                {
                    throw new CommonException(ErrorCode.Service.Network.P2P_SERVICE_NOT_START);
                }

                var texts = nodeAddress.Split(':');
                int port = 0;

                if (texts.Length != 2 ||
                    !int.TryParse(texts[1], out port) ||
                    port <= 0 ||
                    port >= 65536)
                {
                    throw new CommonException(ErrorCode.Service.Network.NODE_ADDRESS_FORMAT_INVALID);
                }

                var node = p2pComponent.GetNodeByAddress(texts[0], port);

                if(node != null)
                {
                    var result = new GetAddedNodeInfoOM();
                    result.address = nodeAddress;


                    if(node.IsConnected)
                    {
                        result.connected = (node.IsInbound ? "Inbound" : "Outbound");
                        result.connectedTime = node.ConnectedTime;
                    }
                    else
                    {
                        result.connected = "false";
                    }

                    return Ok(result);
                }
                else
                {
                    return Ok();
                }

            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult DisconnectNode(string nodeAddress)
        {
            try
            {
                var texts = nodeAddress.Split(':');
                int port = 0;

                if (texts.Length != 2 ||
                    !int.TryParse(texts[1], out port) ||
                    port <= 0 ||
                    port >= 65536)
                {
                    throw new CommonException(ErrorCode.Service.Network.NODE_ADDRESS_FORMAT_INVALID);
                }

                var p2pComponent = new P2PComponent();
                var node = p2pComponent.GetNodeByAddress(texts[0], port);

                if(node != null)
                {
                    p2pComponent.RemoveNode(node.IP, node.Port);
                    return Ok(true);
                }
                else
                {
                    return Ok(false);
                }

            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult SetBan(string nodeAddress, string command)
        {
            try
            {
                var texts = nodeAddress.Split(':');
                int port = 0;

                if (texts.Length != 2 ||
                    !int.TryParse(texts[1], out port) ||
                    port <= 0 ||
                    port >= 65536)
                {
                    throw new CommonException(ErrorCode.Service.Network.NODE_ADDRESS_FORMAT_INVALID);
                }

                var p2pComponent = new P2PComponent();

                if(command.ToLower() == "add")
                {
                    p2pComponent.AddIntoBlackList(texts[0], port, null);
                }
                else if(command.ToLower() == "remove")
                {
                    p2pComponent.RemoveFromBlackList(texts[0], port);
                }
                else
                {
                    throw new CommonException(ErrorCode.Service.Network.SET_BAN_COMMAND_PARAMETER_NOT_SUPPORTED);
                }

                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult ListBanned()
        {
            try
            {
                var result = new P2PComponent().GetBlackList();
                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult ClearBanned()
        {
            try
            {
                new P2PComponent().ClearBlackList();
                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }
        public IRpcMethodResult SetNetworkActive(bool active)
        {
            try
            {
                var p2pComponent = new P2PComponent();
                bool isRuning = p2pComponent.IsRunning();

                if(active && !isRuning)
                {
                    Startup.P2PStartAction();
                }
                else if(!active && isRuning)
                {
                    Startup.P2PStopAction();
                }

                return Ok();
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

        public IRpcMethodResult GetBlockChainInfo()
        {
            try
            {
                var result = new GetBlockChainInfoOM();
                var info = Startup.GetLatestBlockChainInfoFunc();

                result.isRunning = info.IsP2PRunning;
                result.connections = info.ConnectionCount;
                result.localLastBlockHeight = info.LastBlockHeightInCurrentNode;
                result.localLastBlockTime = info.LastBlockTimeInCurrentNode;
                result.tempBlockCount = info.TempBlockCount;
                result.TempBlockHeights = info.TempBlockHeights;
                result.remoteLatestBlockHeight = info.LatestBlockHeightInNetwork;
                result.timeOffset = info.LatestBlockTimeInNetwork - info.LastBlockTimeInCurrentNode;
                result.SyncTasks = new List<DTO.SyncTaskItem>();

                if(info.SyncTasks != null)
                {
                    foreach (var item in info.SyncTasks)
                    {
                        var newItem = new DTO.SyncTaskItem();
                        newItem.IP = item.IP;
                        newItem.Port = item.Port;
                        newItem.StartTime = Time.GetLocalDateTime(item.StartTime);
                        newItem.Status = item.Status;
                        newItem.Heights = item.Heights;

                        result.SyncTasks.Add(newItem);
                    }
                }

                if (info.LastBlockTimeInCurrentNode <= 0 || info.LatestBlockTimeInNetwork <= 0)
                {
                    result.timeOffset = -1;
                }
                

                return Ok(result);
            }
            catch (CommonException ce)
            {
                return Error(ce.ErrorCode, ce.Message, ce);
            }
            catch (Exception ex)
            {
                return Error(ErrorCode.UNKNOWN_ERROR, ex.Message, ex);
            }
        }

    }
}
