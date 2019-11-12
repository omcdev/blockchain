using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace OmniCoin.Messages
{
    public class BlockMsg : BasePayload
    {
        public BlockMsg()
        {
            this.Transactions = new List<TransactionMsg>();
        }


        public string GetPayloadHash()
        {
            var hashsBytes = this.Transactions.Select(tx => Base16.Decode(tx.Hash)).ToList();

            this.Transactions.ForEach(x => { LogHelper.Warn(x.Hash); });
            LogHelper.Warn("\r\n");

            var bytes = MerkleTree.Hash(hashsBytes);

            return Base16.Encode(
                HashHelper.Hash(
                    bytes.ToArray()
            ));
        }

        public BlockHeaderMsg Header { get; set; }
        public List<TransactionMsg> Transactions { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            this.Header = new BlockHeaderMsg();
            this.Header.Deserialize(bytes, ref index);

            var txIndex = 0;
            while (txIndex < this.Header.TotalTransaction)
            {
                var transactionMsg = new TransactionMsg();
                transactionMsg.Deserialize(bytes, ref index);
                this.Transactions.Add(transactionMsg);

                txIndex++;
            }
        }

        public override byte[] Serialize()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Header.Serialize());

            foreach (var tx in Transactions)
            {
                bytes.AddRange(tx.Serialize());
            }

            return bytes.ToArray();
        }

        public override string ToString()
        {
            var key = $"{Header.Hash}_{Header.Height};";
            return key;
        }

        public static bool FilterKeyByHeight(string key, long height)
        {
            var heightFilter = "_" + height + ";";
            return key.Contains(heightFilter);
        }
    }
}