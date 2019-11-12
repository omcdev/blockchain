// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using FiiiChain.Consensus;
using FiiiChain.Framework;

namespace FiiiChain.DataAgent
{
    class BlockPool
    {
        private static BlockPool instance;

        public static void Initialize(long dbHeight, string lastDbBlockHash)
        {
            instance = new BlockPool();
            instance.DBHeight = dbHeight;
            instance.LastDBBlockHash = lastDbBlockHash;
            instance.LatestHeight = dbHeight;
        }

        public BlockPool()
        {
            this.Leaves = new List<BlockPoolItem>();
            this.RootList = new List<BlockPoolItem>();
        }

        private static BlockPool Instance
        {
            get { return instance; }
        }

        protected List<BlockPoolItem> Leaves { get; set; }
        protected List<BlockPoolItem> RootList { get; set; }
        protected long DBHeight { get; set; }
        protected string LastDBBlockHash { get; set; }
        public long LatestHeight { get; set; }

        public void AddBlock(BlockMsg block)
        {
            var newNode = new BlockPoolItem(block);
            //Check with root list
            for (int i = 0; i < this.RootList.Count; i++)
{
                if(RootList[i].Block.Header.PreviousBlockHash == block.Header.Hash)
                {
                    newNode.AddChild(this.RootList[i]);
                    RootList.RemoveAt(i);
                    break;
                }
            }

            //Search in leaves
            for(int i =0; i < this.Leaves.Count;i ++)
            {
                var leaf = this.Leaves[i];

                if (leaf.Block.Header.Hash == block.Header.PreviousBlockHash)
                {
                    //add new node into leaves, and remove parent from leaves
                    leaf.AddChild(newNode);
                    Leaves.RemoveAt(i);
                    break;
                }
            }

            //Search in parents
            if(newNode.Parent == null)
            {
                for (int i = 0; i < Leaves.Count; i++)
                {
                    var leaf = Leaves[i];
                    var result = searchNodeByHash(leaf, block.Header.PreviousBlockHash);

                    if (result != null)
                    {
                        result.AddChild(newNode);
                        //Leaves.Add(child);
                        //Leaves.RemoveAt(i);
                        break;
                    }
                }
            }

            if(newNode.Parent == null)
            {
                this.RootList.Add(newNode);
            }

            if(newNode.Children.Count == 0)
            {
                this.Leaves.Add(newNode);
            }

            this.recalculateDeep();

            if(block.Header.Height > this.LatestHeight)
            {
                this.LatestHeight = block.Header.Height;
            }
        }

        public BlockMsg GetVerifiedBlock()
        {
            try
            {
                BlockPoolItem selectedNode = this.RootList.Where(r => r.Block.Header.PreviousBlockHash == this.LastDBBlockHash).
                    OrderByDescending(r => r.Depth).
                    ThenByDescending(r => r.TotalDifficulty).
                    FirstOrDefault();

                if (selectedNode != null && selectedNode.Depth + 1 > BlockSetting.VERIFIED_BLOCKS)
                {
                    return selectedNode.Block;
                }
                else
                {
                    return null;
                }
            }
            catch(Exception)
            {
                return null;
            }
        }

        public BlockMsg GetLatestBlock()
        {
            BlockPoolItem selectedNode = this.Leaves.OrderByDescending(l => l.Block.Header.Height).
                ThenBy(l => l.Block.Header.Timestamp).FirstOrDefault();

            if (selectedNode != null)
            {
                return selectedNode.Block;
            }
            else
            {
                return null;
            }
        }

        public void TakeOutVerifiedBlock()
        {
            //get the node with max depth in the root list
            BlockPoolItem selectedNode = this.RootList.Where(r => r.Block.Header.PreviousBlockHash == this.LastDBBlockHash).
                OrderByDescending(r => r.Depth).
                ThenByDescending(r => r.TotalDifficulty).
                FirstOrDefault();

            if(selectedNode.Children.Count == 0)
            {
                return;
            }

            for (int i = this.RootList.Count; i > 0; i--)
            {
                var root = this.RootList[i - 1];

                //remove the nodes that block height same with or less than selected node
               if (root.Block.Header.PreviousBlockHash == selectedNode.Block.Header.PreviousBlockHash ||
                    root.Block.Header.Height <= selectedNode.Block.Header.Height)
                {
                    if(root.Block.Header.Hash == selectedNode.Block.Header.Hash)
                    {
                        //get the max depth child node
                        var childNode = selectedNode.Children.
                            OrderByDescending(c => c.Depth).
                            ThenByDescending(r => r.TotalDifficulty).
                            FirstOrDefault();
                        this.RootList[i - 1] = childNode;


                        for (int j = root.Children.Count; j > 0; j--)
                        {
                            var child = root.Children[j - 1];

                            //remove other children nodes
                            if (child.Block.Header.Hash != childNode.Block.Header.Hash)
                            {
                                var leaves = new List<BlockPoolItem>();
                                this.GetLeavesByNode(child, leaves);

                                //remove leaves of current child node
                                foreach (var leaf in leaves)
                                {
                                    this.Leaves.Remove(leaf);
                                }
                            }

                            root.Children.RemoveAt(j - 1);
                            child = null;
                        }

                    }
                    else
                    {
                        var leaves = new List<BlockPoolItem>();
                        this.GetLeavesByNode(root, leaves);

                        //remove the leaves of current node
                        foreach (var leaf in leaves)
                        {
                            this.Leaves.Remove(leaf);
                        }

                        this.RootList.RemoveAt(i - 1);
                        root = null;
                    }

                    break;
                }
            }

            //update db block hash and height
            this.LastDBBlockHash = selectedNode.Block.Header.Hash;
            this.DBHeight = selectedNode.Block.Header.Height;
        }

        public BlockMsg SearchBlockByHash(string hash)
        {
            BlockPoolItem result = null;

            foreach (var node in Leaves)
            {
                result = searchNodeByHash(node, hash);

                if (result != null)
                {
                    break;
                }
            }

            if(result != null)
            {
                return result.Block;
            }
            else
            {
                return null;
            }
        }

        private BlockPoolItem searchNodeByHash(BlockPoolItem node, string prevHash)
        {
            if(node.Block.Header.Hash == prevHash)
            {
                return node;
            }
            else
            {
                if(node.Parent != null)
                {
                    return searchNodeByHash(node.Parent, prevHash);
                }
                else
                {
                    return null;
                }
            }
        }

        public BlockPoolItem GetRootByNode(BlockPoolItem node)
        {
            if(node.Parent == null)
            {
                return node;
            }
            else
            {
                return GetRootByNode(node.Parent);
            }
        }

        public void GetLeavesByNode(BlockPoolItem node, List<BlockPoolItem> leaves)
        {
            if(node.Children.Count == 0)
            {
                this.Leaves.Add(node);
            }
            else
            {
                foreach(var child in node.Children)
                {
                    this.GetLeavesByNode(child, leaves);
                }
            }
        }

        public Dictionary<string, List<OutputMsg>> GetTransactionOutputsByAccountId(string accountId)
        {
            var dict = new Dictionary<string, List<OutputMsg>>();

            foreach(var root in this.RootList)
            {
                getOutputsByAccountId(root, accountId, dict);
            }

            return dict;
        }

        private void getOutputsByAccountId(BlockPoolItem node, string accountId, Dictionary<string, List<OutputMsg>> dict)
        {
            foreach(var tx in node.Block.Transactions)
            {
                if(!dict.ContainsKey(tx.Hash))
                {
                    dict.Add(tx.Hash, new List<OutputMsg>());
                }

                foreach(var output in tx.Outputs)
                {
                    var publicKeyHash = Script.GetPublicKeyHashFromLockScript(output.LockScript);
                    var id = AccountIdHelper.CreateAccountAddressByPublicKeyHash(Base16.Decode(publicKeyHash));

                    if(id == accountId)
                    {
                        dict[tx.Hash].Add(output);
                    }
                }
            }

            foreach(var child in node.Children)
            {
                getOutputsByAccountId(child, accountId, dict);
            }
        }

        public OutputMsg GetOutputMsg(string outputTxHash, int outputIndex, out long blockHeight)
        {
            OutputMsg msg = null;
            long height;

            foreach(var root in this.RootList)
            {
                msg = this.getOutputMsg(root, outputTxHash, outputIndex, out height);

                if(msg != null)
                {
                    blockHeight = height;
                    return msg;
                }
            }

            blockHeight = -1;
            return null;
        }

        private OutputMsg getOutputMsg(BlockPoolItem node, string outputTxHash, int outputIndex, out long blockHeight)
        {
            foreach (var tx in node.Block.Transactions)
            {
                if (tx.Hash == outputTxHash && tx.Outputs.Count > outputIndex)
                {
                    var msg = tx.Outputs[outputIndex];
                    blockHeight = node.Block.Header.Height;
                    return msg;
                }
            }

            foreach (var child in node.Children)
            {
                long height;
                var msg = this.getOutputMsg(child, outputTxHash, outputIndex, out height);

                if(msg != null)
                {
                    blockHeight = height;
                    return msg;
                }
            }

            blockHeight = -1;
            return null;
        }

        public bool CheckUTXOSpent(string currentTxHash, string outputTxHash, int outputIndex)
        {
            foreach (var root in this.RootList)
            {
                var result = this.checkUTXOSpent(root, currentTxHash, outputTxHash, outputIndex);

                if(result)
                {
                    return true;
                }
            }

            return false;
        }

        private bool checkUTXOSpent(BlockPoolItem node, string currentTxHash, string outputTxHash, int outputIndex)
        {
            foreach (var tx in node.Block.Transactions)
            {
                if(tx.Hash == currentTxHash)
                {
                    continue;
                }

                foreach(var input in tx.Inputs)
                {
                    if(input.OutputTransactionHash == outputTxHash && input.OutputIndex == outputIndex)
                    {
                        return true;
                    }
                }
            }

            foreach (var child in node.Children)
            {
                var result = this.checkUTXOSpent(child, currentTxHash, outputTxHash, outputIndex);

                if (result)
                {
                    return true;
                }
            }

            return false;
        }

        private void recalculateDeep()
        {
            foreach(var leaf in this.Leaves)
            {
                this.recalculateDeep(leaf, 0);

                this.reculculateTotalDifficulty(leaf, new POW(leaf.Block.Header.Height).ConvertBitsToDifficulty(leaf.Block.Header.Bits));
            }
        }

        private void recalculateDeep(BlockPoolItem node, int deep)
        {
            node.Depth = deep;

            if(node.Parent != null)
            {
                recalculateDeep(node.Parent, deep + 1);
            }
        }

        private void reculculateTotalDifficulty(BlockPoolItem node, long difficulty)
        {
            node.TotalDifficulty = difficulty;

            if(node.Parent != null)
            {
                reculculateTotalDifficulty(node.Parent, difficulty + new POW(node.Parent.Block.Header.Height).ConvertBitsToDifficulty(node.Parent.Block.Header.Bits));
            }
        }

        private int Count()
        {
            int totalCount = 0;

            foreach(var root in this.RootList)
            {
                countNodes(root, ref totalCount);
            }

            return totalCount;
        }

        private void countNodes(BlockPoolItem node, ref int count)
        {
            count++;

            foreach (var child in node.Children)
            {
                countNodes(child, ref count);
            }
        }

        public TransactionMsg GetTransactionByHash(string transactionHash)
        {
            TransactionMsg msg = null;

            foreach (var root in this.RootList)
            {
                msg = this.getTransactionByHash(root, transactionHash);

                if (msg != null)
                {
                    return msg;
                }
            }

            return null;
        }
        private TransactionMsg getTransactionByHash(BlockPoolItem node, string transactionHash)
        {
            foreach (var tx in node.Block.Transactions)
            {
                if (tx.Hash == transactionHash)
                {
                    return tx;
                }
            }

            foreach (var child in node.Children)
            {
                var msg = this.getTransactionByHash(child, transactionHash);

                if (msg != null)
                {
                    return msg;
                }
            }

            return null;
        }

        public BlockMsg GetBlockByHash(string hash)
        {
            BlockMsg msg = null;

            foreach (var root in this.RootList)
            {
                msg = this.getBlockByHash(root, hash);

                if (msg != null)
                {
                    return msg;
                }
            }

            return null;
        }
        private BlockMsg getBlockByHash(BlockPoolItem node ,string hash)
        {
            if(node.Block.Header.Hash == hash)
            {
                return node.Block;
            }
            else
            {
                foreach(var child in node.Children)
                {
                    var msg = this.getBlockByHash(child, hash);

                    if(msg != null)
                    {
                        return msg;
                    }
                }
            }

            return null;
        }

        public BlockMsg GetBlockByHeight(long height)
        {
            var items = new List<BlockPoolItem>();

            foreach(var root in this.RootList)
            {
                this.getBlockByHeight(root, height, items);
            }

            var item = items.OrderByDescending(i => i.Depth).ThenByDescending(i => i.TotalDifficulty).FirstOrDefault();

            if(item != null)
            {
                return item.Block;
            }
            else
            {
                return null;
            }
        }

        private void getBlockByHeight(BlockPoolItem item, long height, List<BlockPoolItem> items)
        {
            if(item.Block.Header.Height == height)
            {
                items.Add(item);
            }
            else
            {
                foreach(var child in item.Children)
                {
                    this.getBlockByHeight(child, height, items);
                }
            }
        }
    }
}
