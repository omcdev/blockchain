

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Business;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ThreadState = System.Threading.ThreadState;

namespace OmniCoin.Node
{
    public class P2PJob : BaseJob
    {
        P2PComponent p2pComponent;
        TransactionComponent txComponent;
        BlockComponent blockComponent;
        System.Timers.Timer blockSyncTimer;
        System.Timers.Timer txSyncTimer;
        System.Timers.Timer blockSaveTimer;
        Thread thread;
        //Thread threadProcessLongTimeCommand;
        //private ConcurrentQueue<P2PState> longTimeCommandQueue = new ConcurrentQueue<P2PState>();
        bool isRunning;
        bool needSyncTxPool = false;
        //bool needSendHeartbeat = false;
        Guid Identity = Guid.Empty;

        //type defined: 1st string: transaction hash, 2nd long: sync time
        Dictionary<string, long> txsInSynchronizing = new Dictionary<string, long>();
        //type defined: 1st long: block height, 2nd long: sync time
        //Dictionary<long, long> blocksInSynchronizing = new Dictionary<long, long>();
        BlockSyncManager syncManager;
        List<long> newBlocksInDownloading = new List<long>();
        List<string> newTxInDownloading = new List<string>();
        List<BlockMsg> tempBlockList = new List<BlockMsg>();
        List<string> blockedTxHashList = new List<string>();
        List<string> blockedBlockHashList = new List<string>();
        public string LocalIP { get; set; }
        public long LocalHeight = -1;
        public long LocalConfirmedHeight = -1;
        public long LocalLatestBlockTime = 0;
        public long RemoteLatestHeight = -1;
        public long RemoteLatestBlockTime = 0;
        private long localHeightUpdatedTime = 0;
        private long blockSyncSleepTime = 0;
        private bool blockSyncSleep = false;

        public P2PJob()
        {
            this.p2pComponent = new P2PComponent();
            this.txComponent = new TransactionComponent();
            this.blockComponent = new BlockComponent();
            blockSyncTimer = new System.Timers.Timer(10 * 1000);
            blockSyncTimer.AutoReset = true;
            blockSyncTimer.Elapsed += blockSyncTimer_Elapsed;

            txSyncTimer = new System.Timers.Timer(1 * 60 * 1000);
            txSyncTimer.AutoReset = true;
            txSyncTimer.Elapsed += TxSyncTimer_Elapsed;

            blockSaveTimer = new System.Timers.Timer(2 * 1000);
            blockSaveTimer.AutoReset = true;
            blockSaveTimer.Elapsed += BlockSaveTimer_Elapsed;
        }

        private void BlockSaveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            this.blockSaveTimer.Stop();

            try
            {
                var blockList = this.tempBlockList.OrderBy(b => b.Header.Height).ToList();

                for (int i = 0; i < blockList.Count; i++)
                {
                    if (!isRunning)
                    {
                        return;
                    }

                    var item = blockList[i];

                    var targetHeight = GlobalParameters.LocalHeight + 1;
                    if (item.Header.Height < targetHeight)
                    {
                        tempBlockList.RemoveAll(x => x.Header.Height < targetHeight);
                        break;
                    }

                    if (item.Header.Height != targetHeight)
                    {
                        Thread.Sleep(100);
                        break;
                    }

                    var result = this.saveBlockToDB(item);

                    if (!result && i + 1 < blockList.Count)
                    {
                        if (blockList[i + 1].Header.Height > item.Header.Height)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }

            this.blockSaveTimer.Start();
        }

        public override JobStatus Status
        {
            get
            {
                if (thread == null || (thread.ThreadState != ThreadState.Running && thread.ThreadState != ThreadState.WaitSleepJoin))
                {
                    return JobStatus.Stopped;
                }
                else if ((thread.ThreadState == ThreadState.Running || thread.ThreadState == ThreadState.WaitSleepJoin) && !isRunning)
                {
                    return JobStatus.Stopping;
                }
                else
                {
                    return JobStatus.Running;
                }
            }
        }

        public override void Start()
        {
            this.Identity = Guid.NewGuid();
            this.syncManager = new BlockSyncManager(this.Identity.ToString());
            syncManager.TaskFinished = createNewSyncTask;
            isRunning = true;
            blockSyncTimer_Elapsed(null, null);
            thread = new Thread(this.startP2P);
            thread.Start();
            blockSyncTimer.Start();
            txSyncTimer.Start();

            this.blockSaveTimer.Start();
        }

        public override void Stop()
        {
            isRunning = false;
            LogHelper.Warn("exec P2PJob's Stop()");
            blockSyncTimer.Stop();
            txSyncTimer.Stop();
            blockSaveTimer.Stop();
            p2pComponent.P2PStop();
            p2pComponent.RegisterMessageReceivedCallback(null);
            p2pComponent.RegisterNodeConnectedStateChangedCallback(null);
        }

        public BlockChainInfo GetLatestBlockChainInfo()
        {
            var result = new BlockChainInfo();
            result.IsP2PRunning = p2pComponent.IsRunning();

            if (result.IsP2PRunning)
            {
                var nodes = p2pComponent.GetNodes();
                result.ConnectionCount = nodes.Where(n => n.IsConnected).Count();
                result.LastBlockHeightInCurrentNode = this.LocalHeight;
                result.LastBlockTimeInCurrentNode = this.LocalLatestBlockTime;
                result.LatestBlockHeightInNetwork = this.RemoteLatestHeight;
                result.LatestBlockTimeInNetwork = this.RemoteLatestBlockTime;
                result.TempBlockCount = this.tempBlockList.Count;

                var tempHeights = this.tempBlockList.OrderBy(b => b.Header.Height).Select(b => b.Header.Height).ToArray();

                result.TempBlockHeights = string.Join(",", tempHeights);
                result.SyncTasks = new List<SyncTaskItem>();

                foreach (var task in this.syncManager.TaskList)
                {
                    var item = new SyncTaskItem();
                    item.IP = task.NodeIP;
                    item.Port = task.NodePort;
                    item.StartTime = task.StartTime;
                    item.Status = task.Status.ToString();
                    item.Heights = string.Join(",", task.Heights.ToArray());

                    result.SyncTasks.Add(item);
                }
            }

            return result;
        }

        private void startP2P()
        {
            p2pComponent.RegisterMessageReceivedCallback(this.dataReceived);
            p2pComponent.RegisterNodeConnectedStateChangedCallback(this.connectionStateChanged);
            int port = GlobalParameters.IsTestnet? ConfigCenter.ConfigNode.P2PPortTestNet:ConfigCenter.ConfigNode.P2PPortMainNet;            
            p2pComponent.P2PStart(this.Identity, Convert.ToString(ConfigCenter.ConfigNode.LocalIP), port, false);
        }

        #region 注释,暂时不使用

        //private void processLongTimeCommand()
        //{
        //    LogHelper.Warn("Thread : In to thread threadProcessLongTimeCommand's process action : processLongTimeCommand");
        //    P2PState state = default(P2PState);

        //    //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        //    while (this.isRunning)
        //    {                
        //        if (longTimeCommandQueue.TryDequeue(out state))
        //        {
        //            if(state != null)
        //            {
        //                try
        //                {
        //                    //stopwatch.Reset();
        //                    //stopwatch.Start();
        //                    int index = 0;
        //                    switch (state.Command.CommandName)
        //                    {
        //                        case CommandNames.Block.GetHeaders:
        //                            var getHeadersMsg = new GetHeadersMsg();
        //                            getHeadersMsg.Deserialize(state.Command.Payload, ref index);
        //                            this.receivedGetHeaders(state.IP, state.Port, getHeadersMsg, state.Command.Nonce);
        //                            break;
        //                        case CommandNames.Block.GetBlocks:
        //                            var getBlocksMsg = new GetBlocksMsg();
        //                            getBlocksMsg.Deserialize(state.Command.Payload, ref index);
        //                            this.receivedGetBlocks(state.IP, state.Port, getBlocksMsg, state.Command.Nonce);
        //                            break;
        //                        default:
        //                            break;
        //                    }
        //                    //stopwatch.Stop();
        //                    //LogHelper.Warn($"processLongTimeCommand : {state.Command.CommandName}  -- {stopwatch.ElapsedMilliseconds}  , longTimeCommandQueue count = {longTimeCommandQueue.Count()}" );

        //                }
        //                catch (Exception ex)
        //                {
        //                    LogHelper.Error(ex.ToString());
        //                    //LogHelper.Warn($"processLongTimeCommand : {state.Command.CommandName}  -- {stopwatch.ElapsedMilliseconds}  , longTimeCommandQueue count = {longTimeCommandQueue.Count()} --　Exception msg: {ex.ToString()} ");
        //                }

        //            }
        //            Thread.Sleep(10);
        //        }
        //        else
        //        {
        //            Thread.Sleep(50);
        //        }

        //    }
        //    LogHelper.Warn("Thread : out of thread threadProcessLongTimeCommand's process action : processLongTimeCommand");
        //}

        #endregion

        private void dataReceived(P2PState state)
        {
            int index = 0;

            switch (state.Command.CommandName)
            {
                case CommandNames.Transaction.GetTxPool:
                    this.receivedGetTransactionPool(state.IP, state.Port, state.Command.Nonce);
                    break;
                case CommandNames.Transaction.TxPool:
                    var txPoolMsg = new TxPoolMsg();
                    txPoolMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedTransacitonPoolMessage(state.IP, state.Port, txPoolMsg);
                    break;
                case CommandNames.Transaction.GetTx:
                    var getTxMsg = new GetTxsMsg();
                    getTxMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedGetTransaction(state.IP, state.Port, getTxMsg, state.Command.Nonce);
                    break;
                case CommandNames.Transaction.Tx:
                    var txsMsg = new TxsMsg();
                    txsMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedTransactionMessage(state.IP, state.Port, txsMsg);
                    break;
                case CommandNames.Transaction.NewTx:
                    var newTxMsg = new NewTxMsg();
                    newTxMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedNewTransactionMessage(state.IP, state.Port, newTxMsg);
                    break;
                case CommandNames.Block.GetHeight:
                    this.receivedGetHeight(state.IP, state.Port, state.Command.Nonce);
                    break;
                case CommandNames.Block.Height:
                    var heightMsg = new HeightMsg();
                    heightMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedHeightMessage(state.IP, state.Port, heightMsg);
                    break;
                case CommandNames.Block.GetHeaders:
                    var getHeadersMsg = new GetHeadersMsg();
                    getHeadersMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedGetHeaders(state.IP, state.Port, getHeadersMsg, state.Command.Nonce);
                    //longTimeCommandQueue.Enqueue(state);
                    break;
                case CommandNames.Block.Headers:
                    var headersMsg = new HeadersMsg();
                    headersMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedHeadersMessage(state.IP, state.Port, headersMsg);
                    break;
                case CommandNames.Block.GetBlocks:
                    var getBlocksMsg = new GetBlocksMsg();
                    getBlocksMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedGetBlocks(state.IP, state.Port, getBlocksMsg, state.Command.Nonce);
                    //longTimeCommandQueue.Enqueue(state);
                    break;
                case CommandNames.Block.Blocks:
                    var blocksMsg = new BlocksMsg();
                    blocksMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedBlocksMessage(state.IP, state.Port, blocksMsg);
                    break;
                case CommandNames.Block.NewBlock:
                    var newBlockMsg = new NewBlockMsg();
                    newBlockMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedNewBlockMessage(state.IP, state.Port, newBlockMsg, state.Command.Nonce);
                    break;
                case CommandNames.MiningPool.GetMiningPools:
                    this.receivedGetMiningPoolsMessage(state.IP, state.Port);
                    break;
                case CommandNames.MiningPool.MiningPools:
                    var miningPoolMsg = new MiningPoolMsg();
                    miningPoolMsg.Deserialize(state.Command.Payload, ref index);
                    receivedMiningPoolsMessage(miningPoolMsg);
                    break;
                case CommandNames.MiningPool.NewMiningPool:
                    var newMiningPoolMsg = new NewMiningPoolMsg();
                    newMiningPoolMsg.Deserialize(state.Command.Payload, ref index);
                    this.receivedNewMiningPoolMessage(state, newMiningPoolMsg);
                    break;
                case CommandNames.Other.Reject:
                case CommandNames.Other.NotFound:
                default:
                    break;
            }
        }
        private void getAddrMsgHandle(P2PState state)
        {
            var peers = this.p2pComponent.GetNodes();
            var peer = peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                var data = new GetAddrMsg();
                int index = 0;
                data.Deserialize(state.Command.Payload, ref index);

                if (data.Count <= 0 || data.Count > 100)
                {
                    data.Count = 100;
                }

                var list = peers.Where(p => p.IP != state.IP || p.Port != state.Port).OrderByDescending(p => p.LastHeartbeat).Take(data.Count).ToList();

                var payload = new AddrMsg();

                foreach (var item in list)
                {
                    payload.AddressList.Add(new AddrMsg.AddressInfo()
                    {
                        Ip = item.IP,
                        Port = item.Port,
                        Identity = item.Identity
                    });
                }

                var addrCommand = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.P2P.Addr, payload);
                this.p2pComponent.SendCommand(state.IP, state.Port, addrCommand);
            }
        }
        private void addrMsgHandle(P2PState state)
        {
            var peers = this.p2pComponent.GetNodes();
            var peer = peers.Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                var payload = new AddrMsg();
                int index = 0;
                payload.Deserialize(state.Command.Payload, ref index);

                foreach (var item in payload.AddressList)
                {
                    if (peers.Where(p => !p.IsTrackerServer && p.IP == item.Ip && p.Port == item.Port && p.IsConnected).Count() == 0)
                    {
                        this.p2pComponent.AddNode(item.Ip, item.Port);
                    }
                }
            }

        }
        private void heartbeatMsgHandle(P2PState state)
        {
            var peer = this.p2pComponent.GetNodes().Where(p => p.IP == state.IP && p.Port == state.Port).FirstOrDefault();

            if (peer != null && peer.IsConnected)
            {
                var payload = new HeightMsg();
                int index = 0;

                try
                {
                    payload.Deserialize(state.Command.Payload, ref index);
                    peer.LatestHeight = payload.Height;
                    peer.LatestBlockTime = payload.BlockTime;
                }
                catch
                {

                }

                peer.LastHeartbeat = Time.EpochTime;
            }
        }
        private void connectionStateChanged(P2PNode node)
        {
            if (node.IsConnected)
            {
                this.sendHeartbeat(node.IP, node.Port);
                this.sendGetMiningPool(node.IP, node.Port);
                this.sendGetTransactionPool(node.IP, node.Port);
            }
        }
        private void TxSyncTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var nodes = this.p2pComponent.GetNodes();
            foreach (var node in nodes)
            {
                if (node.IsConnected)
                {
                    this.sendHeartbeat(node.IP, node.Port);

                    if (this.syncManager.TaskCount == 0 || (this.RemoteLatestHeight > -1 && this.RemoteLatestHeight - this.LocalHeight <= 10))
                    {
                        if (!node.IsTrackerServer && needSyncTxPool)
                        {
                            this.sendGetTransactionPool(node.IP, node.Port);
                        }
                    }
                }
            }

            needSyncTxPool = !needSyncTxPool;
        }
        private void blockSyncTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var nodes = this.p2pComponent.GetNodes();

            var newHeight = this.blockComponent.GetLatestHeight();

            if (newHeight > LocalHeight)
            {
                LocalHeight = newHeight;
                localHeightUpdatedTime = Time.EpochTime;
            }

            LocalConfirmedHeight = this.blockComponent.GetLatestConfirmedHeight();
            this.syncManager.LocalBlockHeight = LocalHeight;
            this.LocalLatestBlockTime = GlobalParameters.LatestBlockTime;

            p2pComponent.SetBlockHeightAndTime(LocalHeight, LocalLatestBlockTime);

            foreach (var node in nodes)
            {
                if (node.IsConnected)
                {
                    if (!node.IsTrackerServer && node.LatestHeight > RemoteLatestHeight)
                    {
                        RemoteLatestHeight = node.LatestHeight;
                        RemoteLatestBlockTime = node.LatestBlockTime;
                    }

                    //this.sendHeartbeat(node.IP, node.Port);
                }
            }

            this.createNewSyncTask();
        }
        private void createNewSyncTask()
        {
            if (RemoteLatestHeight > LocalHeight)
            {
                var random = new Random();
                var nodes = p2pComponent.GetNodes();

                if (syncManager.TaskCount < syncManager.MaxTasks)
                {
                    long startHeight = syncManager.MaxHeight + 1;

                    if (startHeight <= LocalHeight
                        || (!this.tempBlockList.Any(b => b.Header.Height == LocalHeight + 1) &&
                        !this.syncManager.TaskList.Any(t => t.Heights.Contains(LocalHeight + 1)))
                        )
                    {
                        startHeight = LocalHeight + 1;
                    }

                    var removeTasks = syncManager.TaskList.Where(x => x.Heights.Max() < LocalHeight+1).ToList();
                    if (removeTasks.Any())
                    {
                        foreach (var removeTask in removeTasks)
                        {
                            if (removeTask.Status == BlockSyncStatus.GetHeaders || removeTask.Status == BlockSyncStatus.HeaderSyncing)
                            {
                                syncManager.CloseGetHeadersTask(removeTask.NodeIP, removeTask.NodePort);
                            }
                            else
                            {
                                syncManager.CloseGetBlocksTask(removeTask.NodeIP, removeTask.NodePort);
                            }
                        }
                    }

                    long endHeight = startHeight + 14;

                    if (endHeight > RemoteLatestHeight)
                    {
                        endHeight = RemoteLatestHeight;
                    }

                    if (endHeight < startHeight)
                    {
                        return;
                    }

                    if (startHeight > LocalHeight + 1 && tempBlockList.Count > 500)
                    {
                        bool lostBlocks = false;
                        var tempHeights = this.tempBlockList.OrderBy(b => b.Header.Height).Select(b => b.Header.Height).ToArray();

                        for (var i = 0; i < tempHeights.Length; i++)
                        {
                            if (i + 1 < tempHeights.Length)
                            {
                                if (tempHeights[i + 1] > tempHeights[i] + 1)
                                {
                                    if (!this.syncManager.TaskList.Any(t => t.Heights.Contains(tempHeights[i] + 1)))
                                    {
                                        startHeight = tempHeights[i] + 1;
                                        endHeight = startHeight + 14;

                                        if (endHeight >= tempHeights[i + 1])
                                        {
                                            endHeight = tempHeights[i + 1] - 1;
                                        }

                                        lostBlocks = true;
                                    }
                                }

                            }
                        }

                        if (!lostBlocks/* || this.tempBlockList.Count > 500*/)
                        {
                            return;
                        }
                    }

                    List<long> heights = new List<long>();

                    for (var i = startHeight; i <= endHeight; i++)
                    {
                        heights.Add(i);
                    }

                    var nodeList = nodes.Where(n => n.IsConnected &&
                        !n.IsTrackerServer &&
                        n.LatestHeight >= endHeight &&
                        !syncManager.NodeAddressList.Contains(n.IP + ":" + n.Port)).ToList();

                    if (heights.Any(x => x >= 101016 && x <= 101083))
                        nodeList.RemoveAll(x => x.LatestHeight <= 101083);

                    if (nodeList.Count > 0)
                    {
                        var index = random.Next(0, nodeList.Count);
                        var node = nodeList[index];
                        syncManager.CreateGetHeadersTask(heights, node.IP, node.Port);
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }
        private void sendHeartbeat(string address, int port)
        {
            var msg = new HeightMsg();
            msg.Height = this.LocalHeight;
            msg.BlockTime = this.LocalLatestBlockTime;
            var command = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.P2P.Heartbeat, msg);
            p2pComponent.SendCommand(address, port, command);
        }
        private void sendGetTransactionPool(string address, int port)
        {
            P2PCommand cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.GetTxPool, null);
            p2pComponent.SendCommand(address, port, cmd);
        }
        private void sendGetMiningPool(string address, int port)
        {
            P2PCommand cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.MiningPool.GetMiningPools, null);
            p2pComponent.SendCommand(address, port, cmd);
        }
        private void receivedGetTransactionPool(string address, int port, int nonce)
        {
            if (!GlobalParameters.IsPool)
            {
                var hashes = this.txComponent.GetAllHashesFromPool();
                var payload = new TxPoolMsg();
                payload.Hashes.AddRange(hashes);

                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.TxPool, nonce, payload);
                this.p2pComponent.SendCommand(address, port, cmd);
            }
        }
        private void receivedTransacitonPoolMessage(string address, int port, TxPoolMsg msg)
        {
            if (this.syncManager.TaskCount == 0 || (this.RemoteLatestHeight > -1 && this.RemoteLatestHeight - this.LocalHeight <= 10))
            {
                var txHashes = new List<string>();

                LogHelper.Debug("TxPool Hashes Count:" + msg.Hashes.Count);
                foreach (var h in msg.Hashes)
                {
                    if (txHashes.Count > 10)
                    {
                        this.sendGetTransaction(address, port, txHashes);
                        txHashes.Clear();
                    }

                    if (!this.txComponent.CheckBlackTxExisted(h) && !this.txComponent.CheckTxExisted(h))
                    {
                        txHashes.Add(h);
                    }
                }

                if (txHashes.Count > 0)
                {
                    this.sendGetTransaction(address, port, txHashes);
                }
            }
        }
        private void sendGetTransaction(string address, int port, List<string> txHashList)
        {
            if (RemoteLatestHeight - GlobalParameters.LocalHeight > 10)
                return;

            for (int i = txHashList.Count - 1; i >= 0; i--)
            {
                var hash = txHashList[i];

                if (this.txsInSynchronizing.ContainsKey(hash))
                {
                    if (Time.EpochTime - this.txsInSynchronizing[hash] > 60 * 1000 && !this.txComponent.CheckBlackTxExisted(hash))
                    {
                        txsInSynchronizing[hash] = Time.EpochTime;
                    }
                    else
                    {
                        txHashList.RemoveAt(i);
                    }
                }
                else
                {
                    if (!this.txComponent.CheckBlackTxExisted(hash))
                    {
                        txsInSynchronizing.Add(hash, Time.EpochTime);
                    }
                    else
                    {
                        txHashList.RemoveAt(i);
                    }
                }
            }

            if (txHashList.Count > 0)
            {
                var payload = new GetTxsMsg();
                payload.Hashes.AddRange(txHashList);

                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.GetTx, payload);
                p2pComponent.SendCommand(address, port, cmd);
            }
        }
        private void receivedGetTransaction(string address, int port, GetTxsMsg msg, int nonce)
        {
            if (!GlobalParameters.IsPool)
            {
                var txList = new List<TransactionMsg>();

                foreach (var hash in msg.Hashes)
                {
                    var tx = this.txComponent.GetTransactionMsgByHash(hash);

                    if (tx != null)
                    {
                        txList.Add(tx);
                    }
                }

                if (txList.Count > 0)
                {
                    var payload = new TxsMsg();
                    payload.Transactions.AddRange(txList);

                    var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.Tx, payload);
                    this.p2pComponent.SendCommand(address, port, cmd);
                }
                else
                {
                    this.sendDataNoFoundCommand(address, port, nonce);
                }
            }
        }
        private void receivedTransactionMessage(string address, int port, TxsMsg msg)
        {
            foreach (var tx in msg.Transactions)
            {
                try
                {
                    if (this.txsInSynchronizing.ContainsKey(tx.Hash))
                    {
                        txsInSynchronizing.Remove(tx.Hash);
                    }

                    if (this.newTxInDownloading.Contains(tx.Hash))
                    {
                        var nodes = this.p2pComponent.GetNodes();

                        //Broadcast to other node
                        foreach (var node in nodes)
                        {
                            if (node.IsConnected && !node.IsTrackerServer && node.IP != address)
                            {
                                var payload = new NewTxMsg();
                                payload.Hash = tx.Hash;

                                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.NewTx, payload);
                                this.p2pComponent.SendCommand(node.IP, node.Port, cmd);
                            }
                        }

                        newTxInDownloading.Remove(tx.Hash);
                    }

                    this.txComponent.AddTransactionToPool(tx);
                }
                catch
                {

                }
            }
        }
        private void receivedNewTransactionMessage(string address, int port, NewTxMsg msg)
        {
            if (!this.txComponent.CheckBlackTxExisted(msg.Hash) && !this.txComponent.CheckTxExisted(msg.Hash))
            {
                if (!this.newTxInDownloading.Contains(msg.Hash))
                {
                    newTxInDownloading.Add(msg.Hash);

                    var payload = new GetTxsMsg();
                    payload.Hashes.Add(msg.Hash);

                    var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.GetTx, payload);
                    this.p2pComponent.SendCommand(address, port, cmd);
                }
            }
        }
        private void receivedGetMiningPoolsMessage(string address, int port)
        {
            var list = (new MiningPoolComponent()).GetAllMiningPools();

            var payload = new MiningPoolMsg();

            foreach (var item in list)
            {
                MiningMsg itemMsg = new MiningMsg();
                itemMsg.Name = item.Name;
                itemMsg.PublicKey = item.PublicKey;
                itemMsg.Signature = item.Signature;
                payload.MinerInfos.Add(itemMsg);
            }

            var command = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.MiningPool.MiningPools, payload);
            this.p2pComponent.SendCommand(address, port, command);
        }
        private void receivedMiningPoolsMessage(MiningPoolMsg msg)
        {
            var newMsgs = (new MiningPoolComponent()).UpdateMiningPools(msg.MinerInfos);
            if (newMsgs == null || !newMsgs.Any())
                return;
            var nodes = this.p2pComponent.GetNodes();
            nodes.ForEach(peer =>
            {
                if (!peer.IsTrackerServer)
                {
                    var command = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.MiningPool.MiningPools, msg);
                    this.p2pComponent.SendCommand(peer.IP, peer.Port, command);
                }
            });
        }
        private void receivedNewMiningPoolMessage(P2PState state, NewMiningPoolMsg msg)
        {
            if (msg == null || state == null)
                return;
            if (new MiningPoolComponent().AddMiningToPool(msg.MinerInfo))
            {
                var nodes = this.p2pComponent.GetNodes();
                nodes.ForEach(peer =>
                {
                    if (!peer.IsTrackerServer)
                    {
                        var command = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.MiningPool.NewMiningPool, msg);
                        this.p2pComponent.SendCommand(peer.IP, peer.Port, command);
                    }
                });
            }
        }
        public void BroadcastNewTransactionMessage(string txHash)
        {
            if (!GlobalParameters.IsPool)
            {
                var nodes = this.p2pComponent.GetNodes();

                //Broadcast to other node
                foreach (var node in nodes)
                {
                    if (node.IsConnected && !node.IsTrackerServer)
                    {
                        var payload = new NewTxMsg();
                        payload.Hash = txHash;

                        var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Transaction.NewTx, payload);
                        this.p2pComponent.SendCommand(node.IP, node.Port, cmd);
                    }
                }
            }
        }
        public void SendGetHeight(string address, int port)
        {
            var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.GetHeight, null);
            p2pComponent.SendCommand(address, port, cmd);
        }
        private void receivedGetHeight(string address, int port, int nonce)
        {
            var height = this.blockComponent.GetLatestHeight();
            var block = this.blockComponent.GetBlockMsgByHeight(height);

            if (block != null)
            {
                var payload = new HeightMsg();
                payload.Height = height;
                payload.BlockTime = block.Header.Timestamp;

                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.Height, nonce, payload);
                this.p2pComponent.SendCommand(address, port, cmd);
            }
        }
        private void receivedHeightMessage(string address, int port, HeightMsg msg)
        {
            //var localHeight = this.blockComponent.GetLatestHeight();
            //if(localHeight < msg.Height)
            //{
            //    var 
            //}

            var nodes = this.p2pComponent.GetNodes();

            var node = nodes.Where(n => n.IP == address && n.Port == port).FirstOrDefault();

            if (node != null)
            {
                node.LatestHeight = msg.Height;
                node.LatestBlockTime = msg.BlockTime;
            }
        }
        private void receivedGetHeaders(string address, int port, GetHeadersMsg msg, int nonce)
        {
            var headers = this.blockComponent.GetBlockHeaderMsgByHeights(msg.Heights);

            if (headers.Count > 0)
            {
                var payload = new HeadersMsg();
                payload.Headers.AddRange(headers);

                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.Headers, nonce, payload);
                this.p2pComponent.SendCommand(address, port, cmd);
            }
            else
            {
                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Other.NotFound, nonce, null);
                this.p2pComponent.SendCommand(address, port, cmd);
            }
        }
        private void receivedHeadersMessage(string address, int port, HeadersMsg msg)
        {
            var heightList = new List<long>();
            var hashList = new List<string>();

            foreach (var header in msg.Headers)
            {
                if (header.Height > GlobalParameters.LocalHeight &&
                    !this.tempBlockList.Any(b => b.Header.Hash == header.Hash) &&
                    !this.syncManager.TaskList.Any(t => (t.Status == BlockSyncStatus.BlockSyncing || t.Status == BlockSyncStatus.GetBlocks) && t.Hashes.Contains(header.Hash)) &&
                    !blockedBlockHashList.Contains(header.Hash))
                {
                    if (!hashList.Contains(header.Hash))
                    {
                        heightList.Add(header.Height);
                        hashList.Add(header.Hash);
                    }
                }
            }

            if (hashList.Count > 0)
            {
                this.syncManager.CreateGetBlocksTask(heightList, hashList, address, port);
            }
            else
            {
                LogHelper.Debug($"Clear getBlocks task from {address}:{port} : {string.Join(",", msg.Headers.Select(h => h.Height).ToArray())}");
                this.syncManager.CloseGetHeadersTask(address, port);
            }
        }
        private void sendGetBlocks(string address, int port, List<long> heightList)
        {
            if (heightList.Count > 0)
            {
                var payload = new GetBlocksMsg();
                payload.Heights.AddRange(heightList);

                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.GetBlocks, payload);
                p2pComponent.SendCommand(address, port, cmd);
            }
        }

        private void receivedGetBlocks(string address, int port, GetBlocksMsg msg, int nonce)
        {
            List<BlockMsg> blocks;
            try
            {
                blocks = this.blockComponent.GetBlockMsgByHeights(msg.Heights);
            }
            catch (Exception ex)
            {
                LogHelper.Error("GetBlocksMsg Message is Empty");
                throw;
            }

            if (blocks.Count > 0)
            {
                int maxLength = 1000 * 1024; //max 100KB;
                int totalLength = 0;
                var payload = new BlocksMsg();

                foreach (var block in blocks)
                {
                    totalLength += block.Serialize().Length;

                    if (payload.Blocks.Count == 0 || totalLength <= maxLength)
                    {
                        payload.Blocks.Add(block);
                    }
                    else
                    {
                        break;
                    }
                }

                try
                {
                    var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.Blocks, nonce, payload);
                    this.p2pComponent.SendCommand(address, port, cmd);
                }
                catch (Exception ex)
                {
                    LogHelper.Error("GetBlocksMsg Result is Empty");
                }
            }
            else
            {
                var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Other.NotFound, nonce, null);
                this.p2pComponent.SendCommand(address, port, cmd);
            }
        }

        private void receivedBlocksMessage(string address, int port, BlocksMsg msg)
        {
            this.syncManager.CloseGetBlocksTask(address, port);

            foreach (var block in msg.Blocks)
            {
                try
                {
                    if (!this.tempBlockList.Any(t => t.Header.Hash == block.Header.Hash) &&
                        block.Header.Hash == block.Header.GetHash() && block.Header.Height > GlobalParameters.LocalHeight)
                    {
                        this.tempBlockList.Add(block);
                    }
                }
                catch
                {
                    LogHelper.Error(string.Format("Error IP at {0}:{1}", address, port));
                }

                if (this.newBlocksInDownloading.Contains(block.Header.Height))
                {
                    var nodes = this.p2pComponent.GetNodes();
                    var newMsg = new NewBlockMsg();
                    newMsg.Header = block.Header;

                    //Broadcast to other node
                    foreach (var node in nodes)
                    {
                        if (node.IsConnected && !node.IsTrackerServer && node.IP != address)
                        {
                            var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.NewBlock, newMsg);
                            this.p2pComponent.SendCommand(node.IP, node.Port, cmd);
                        }
                    }

                    newBlocksInDownloading.Remove(block.Header.Height);
                }
            }
        }
        public void BroadcastNewBlockMessage(BlockHeaderMsg blockHeader)
        {
            var nodes = this.p2pComponent.GetNodes();

            foreach (var node in nodes)
            {
                if (node.IsConnected && !node.IsTrackerServer)
                {
                    var payload = new NewBlockMsg();
                    payload.Header = blockHeader;

                    var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.NewBlock, payload);
                    this.p2pComponent.SendCommand(node.IP, node.Port, cmd);
                }
            }

        }
        private void receivedNewBlockMessage(string address, int port, NewBlockMsg msg, int nonce)
        {
            if (RemoteLatestHeight - LocalHeight <= 10 && msg.Header.Height - LocalHeight <= 10 && !this.tempBlockList.Any(b => b.Header.Height == msg.Header.Height) && !this.blockComponent.CheckBlockExists(msg.Header.Hash))
            {
                if (!this.newBlocksInDownloading.Contains(msg.Header.Height) && !blockedBlockHashList.Contains(msg.Header.Hash))
                {
                    this.newBlocksInDownloading.Add(msg.Header.Height);

                    var payload = new GetBlocksMsg();
                    payload.Heights.Add(msg.Header.Height);

                    var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Block.GetBlocks, nonce, payload);
                    this.p2pComponent.SendCommand(address, port, cmd);
                }
            }
        }
        private void sendDataNoFoundCommand(string address, int port, int nonce)
        {
            var cmd = P2PCommand.CreateCommand(this.Identity.ToString(), CommandNames.Other.NotFound, nonce, null);
        }
        private bool saveBlockToDB(BlockMsg block)
        {
            try
            {
                var result = this.blockComponent.SaveBlockIntoDB(block);

                if (!result)
                {
                    this.tempBlockList.Remove(block);
                    return false;
                }
                this.LocalHeight = block.Header.Height;
                GlobalParameters.LatestBlockTime = block.Header.Timestamp;
                this.LocalLatestBlockTime = block.Header.Timestamp;
                var items = this.tempBlockList.Where(t => t.Header.Height == block.Header.Height);

                if (items.Any())
                {
                    this.tempBlockList.RemoveAll(item => items.Contains(item));
                }
            }
            catch (CommonException ex)
            {
                this.tempBlockList.Remove(block);
                LogHelper.Error(ex.Message, ex);
            }
            catch (Exception ex)
            {
                //this.tempBlockList.Remove(block);
                LogHelper.Error(ex.Message, ex);
                throw ex;
            }
            return true;
        }
    }
}