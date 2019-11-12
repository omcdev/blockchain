


using OmniCoin.Messages;
using System.Linq;

namespace OmniCoin.Data.Dacs
{
    public class TransactionDac : BlockDbBase<TransactionDac>
    {
        public BlockMsg GetBlockByTxHash(string hash)
        {
            var set = UtxoSetDac.Default.Get(hash, 0);
            if (set == null)
                return null;
            return BlockDac.Default.SelectByHeight(set.BlockHeight);
        }

        public TransactionMsg GetTransaction(string hash)
        {
            var block= GetBlockByTxHash(hash);
            if (block == null)
                return null;
            return block.Transactions.FirstOrDefault(x => x.Hash == hash);
        }

        public bool HasTransaction(string hash)
        {
            return GetTransaction(hash) != null;
        }

        /*
        public virtual IEnumerable<LatestBlockDataEx> GetLatestBlock(int num)
        {
            LatestBlockDataEx result = new LatestBlockDataEx();
            BlockDomain.Get
        }
        */
    }
}