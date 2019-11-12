using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Node
{
    class BlockSyncTask
    {
        public BlockSyncTask()
        {
            this.LastMsgTime = null;
            //this.Status = BlockSyncStatus.Wait;
            //this.Data = new List<byte>();
        }

        public List<long> Heights { get; set; }
        public List<string> Hashes { get; set; }
        public string NodeIP { get; set; }
        public int NodePort { get; set; }
        public long StartTime { get; set; }
        public long? LastMsgTime { get; set; }
        public BlockSyncStatus Status { get; set; }
        //public List<byte> Data { get; set; }
        //public int PieceCount { get; set; }
        //public int PieceIndex { get; set; }
    }

    enum BlockSyncStatus
    {
        GetHeaders,
        GetBlocks,
        HeaderSyncing,
        BlockSyncing,
        Fail,
        Finished
    }
}
