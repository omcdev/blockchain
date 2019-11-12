// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace FiiiChain.DataAgent
{
    public class BlockPoolItem
    {
        public BlockPoolItem()
        {
            this.Children = new List<BlockPoolItem>();
        }

        public BlockPoolItem(BlockMsg block)
        {
            this.Children = new List<BlockPoolItem>();
            this.Block = block;
        }

        public BlockPoolItem AddChild(BlockPoolItem child)
        {
            child.Parent = this;
            this.Children.Add(child);

            return child;
        }

        public int Depth { get; set; }
        public long TotalDifficulty { get; set; }
        public BlockPoolItem Parent { get; set; }
        public List<BlockPoolItem> Children { get; set; }
        public BlockMsg Block { get; set; }
    }
}
