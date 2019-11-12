using OmniCoin.Business;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace OmniCoin.Node
{
    class BlockSyncManager
    {
        bool isRunning = false;
        string nodeId;
        List<string> hashList;
        Timer checkTimer;
        P2PComponent p2pComponent;
        List<BlockSyncTask> removeTasks = new List<BlockSyncTask>();

        public List<BlockSyncTask> TaskList;
        public List<string> NodeAddressList;
        public int MaxTasks = 5;
        public long LocalBlockHeight;
        public long MaxHeight { get; set; }
        public int TaskCount
        {
            get
            {
                return this.TaskList.Count;
            }
        }
        public Action TaskFinished;

        public BlockSyncManager(string id)
        {
            nodeId = id;
            TaskList = new List<BlockSyncTask>();
            hashList = new List<string>();
            NodeAddressList = new List<string>();
            p2pComponent = new P2PComponent();
            MaxHeight = 0;

            isRunning = true;
            checkTimer = new Timer();
            checkTimer.AutoReset = true;
            checkTimer.Interval = 1000;
            checkTimer.Elapsed += CheckTimer_Elapsed;
            checkTimer.Start();
        }

        private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            checkTimer.Stop();

            try
            {
                var nodes = p2pComponent.GetNodes();
                var currentTime = Time.EpochTime;
                this.removeTasks = new List<BlockSyncTask>();

                foreach (var task in TaskList)
                {
                    var node = nodes.FirstOrDefault(p => p.IsConnected && !p.IsTrackerServer && p.IP == task.NodeIP && p.Port == task.NodePort);

                    if (node != null)
                    {
                        if (node.LastCommand == CommandNames.Block.Headers)
                        {
                            if (task.Status == BlockSyncStatus.GetHeaders)
                            {
                                task.Status = BlockSyncStatus.HeaderSyncing;
                            }
                            else
                            {
                                if (currentTime - node.LastReceivedTime > 60 * 1000)
                                {
                                    LogHelper.Info($"Task{task.NodeIP}:{task.NodePort} {task.Status} is fail becuase not received message for a long time. {node.LastReceivedTime}");
                                    task.Hashes = null;
                                    task.Status = BlockSyncStatus.Fail;
                                }
                            }
                        }
                        else if (node.LastCommand == CommandNames.Block.Blocks)
                        {
                            if (task.Status == BlockSyncStatus.GetBlocks)
                            {
                                task.Status = BlockSyncStatus.BlockSyncing;
                            }
                            else
                            {
                                if (currentTime - node.LastReceivedTime > 60 * 1000)
                                {
                                    LogHelper.Info($"Task{task.NodeIP}:{task.NodePort} {task.Status} is fail becuase not received message for a long time. {node.LastReceivedTime}");
                                    task.Status = BlockSyncStatus.Fail;
                                }
                            }
                        }
                        else
                        {
                            if (currentTime - task.StartTime > 2 * 60 * 1000)
                            {
                                LogHelper.Info($"Task{task.NodeIP}:{task.NodePort} {task.Status} is fail becuase not start sync for a long time. {task.StartTime}");
                                task.Status = BlockSyncStatus.Fail;
                            }
                        }
                    }
                    else
                    {
                        task.Status = BlockSyncStatus.Fail;
                    }

                    if (task.Status == BlockSyncStatus.Fail)
                    {
                        //this.CloseTask(task.NodeIP, task.NodePort);
                        if (task.Hashes is null || nodes.Count(p => p.IsConnected && !p.IsTrackerServer) == 0)
                        {
                            this.removeTasks.Add(task);
                        }
                        else
                        {
                            this.removeTasks.Add(task);
                            //this.Resync(task, nodes);
                        }
                    }
                }

                foreach (var task in removeTasks)
                {
                    if (task.Hashes != null)
                    {
                        foreach (var hash in task.Hashes)
                        {
                            this.hashList.Remove(hash);
                        }
                    }

                    var heights = task.Heights.ToArray();
                    Array.Sort(heights);

                    if (heights[heights.Length - 1] == this.MaxHeight)
                    {
                        this.MaxHeight = heights[0] - 1;
                    }

                    NodeAddressList.Remove(task.NodeIP + ":" + task.NodePort);
                    TaskList.Remove(task);
                }

                if (this.removeTasks.Count > 0)
                {
                    this.removeTasks.Clear();

                    if (this.TaskFinished != null)
                    {
                        this.TaskFinished();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.Message, ex);
            }
            finally
            {
                if (isRunning)
                {
                    checkTimer.Start();
                }
            }
        }

        public void CreateGetHeadersTask(List<long> heights, string ip, int port)
        {
            for (int i = 0; i < heights.Count; i++)
            {
                if (heights[i] > MaxHeight)
                {
                    MaxHeight = heights[i];
                }
            }

            var task = new BlockSyncTask();
            task.Hashes = null;
            task.Heights = heights;
            task.NodeIP = ip;
            task.NodePort = port;
            task.StartTime = Time.EpochTime;
            task.LastMsgTime = null;
            task.Status = BlockSyncStatus.GetHeaders;
            TaskList.Add(task);

            if (!NodeAddressList.Contains(ip + ":" + port))
            {
                NodeAddressList.Add(ip + ":" + port);
            }

            var payload = new GetHeadersMsg();
            payload.Heights = heights;
            var cmd = P2PCommand.CreateCommand(nodeId, CommandNames.Block.GetHeaders, payload);
            p2pComponent.SendCommand(ip, port, cmd);

            LogHelper.Debug($"Create new GetHeaders task to {ip}:{port}");
        }

        public void CreateGetBlocksTask(List<long> heights, List<string> hashes, string ip, int port)
        {
            for (int i = 0; i < hashes.Count; i++)
            {
                if (!hashList.Contains(hashes[i]))
                {
                    hashList.Add(hashes[i]);
                }

                if (heights[i] > MaxHeight)
                {
                    MaxHeight = heights[i];
                }
            }

            var task = TaskList.Where(p => p.NodeIP == ip && p.NodePort == port).FirstOrDefault();

            if (task == null)
            {
                task = new BlockSyncTask();
                TaskList.Add(task);
            }

            task.Hashes = hashes;
            task.Heights = heights;
            task.NodeIP = ip;
            task.NodePort = port;
            task.StartTime = Time.EpochTime;
            task.LastMsgTime = null;
            task.Status = BlockSyncStatus.GetBlocks;

            if (!NodeAddressList.Contains(ip + ":" + port))
            {
                NodeAddressList.Add(ip + ":" + port);
            }

            var payload = new GetBlocksMsg();
            payload.Heights = heights;
            var cmd = P2PCommand.CreateCommand(nodeId, CommandNames.Block.GetBlocks, payload);
            p2pComponent.SendCommand(ip, port, cmd);
        }

        public void CloseGetHeadersTask(string ip, int port)
        {
            var task = TaskList.Where(p => p.NodeIP == ip && p.NodePort == port).FirstOrDefault();

            if (task != null && (task.Status == BlockSyncStatus.GetHeaders || task.Status == BlockSyncStatus.HeaderSyncing))
            {
                task.Status = BlockSyncStatus.Finished;

                if (task.Hashes != null)
                {
                    foreach (var hash in task.Hashes)
                    {
                        this.hashList.Remove(hash);
                    }
                }

                NodeAddressList.Remove(ip + ":" + port);
                TaskList.Remove(task);

                if (TaskFinished != null)
                {
                    TaskFinished();
                }
            }
        }

        public void CloseGetBlocksTask(string ip, int port)
        {
            var task = TaskList.Where(p => p.NodeIP == ip && p.NodePort == port).FirstOrDefault();

            if (task != null && (task.Status == BlockSyncStatus.GetBlocks || task.Status == BlockSyncStatus.BlockSyncing))
            {
                task.Status = BlockSyncStatus.Finished;

                if (task.Hashes != null)
                {
                    foreach (var hash in task.Hashes)
                    {
                        this.hashList.Remove(hash);
                    }
                }

                NodeAddressList.Remove(ip + ":" + port);
                TaskList.Remove(task);

                if (TaskFinished != null)
                {
                    TaskFinished();
                }
            }
        }

        public bool ContainsHash(string hash)
        {
            return this.hashList.Contains(hash);
        }

        public bool ContainsNode(string ip, int port)
        {
            return this.NodeAddressList.Contains(ip + ":" + port);
        }

        public void Stop()
        {
            this.isRunning = false;
            this.checkTimer.Stop();
            this.TaskList.Clear();
            this.hashList.Clear();
            this.removeTasks.Clear();
            this.NodeAddressList.Clear();
            this.MaxHeight = 0;
        }
    }
}
