

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Data.Dacs;
using OmniCoin.Entities;
using OmniCoin.Framework;
using System.Collections.Generic;
using System.Linq;

namespace OmniCoin.Business
{
    public class TransactionCommentComponent
    {
        public void Add(string txHash, int outputIndex, string comment)
        {
            var item = new TransactionComment();
            item.TransactionHash = txHash;
            item.OutputIndex = outputIndex;
            item.Comment = comment;
            item.Timestamp = Time.EpochTime;
            var result = 0L;
            TransactionCommentDac.Default.Save(item);
        }

        public TransactionComment GetByTransactionHashAndIndex(string txHash, int outputIndex)
        {
            return TransactionCommentDac.Default.Get(txHash, outputIndex);
        }

        public List<TransactionComment> GetByTransactionHash(string txHash)
        {
            return TransactionCommentDac.Default.SelectByTransactionHash(txHash)?.ToList();
        }
    }
}