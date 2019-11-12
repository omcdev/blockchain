


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities.Explorer
{
    public class BlockInfo
    {
        public string BlockHash { get; set; }
        public long Height;
        public int TradeCount;
        public long TotalAmount;
        public int TotalSize;
    }
}
