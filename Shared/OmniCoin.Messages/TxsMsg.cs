


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class TxsMsg : BasePayload
    {
        public int Count
        {
            get
            {
                return this.Transactions.Count;
            }
        }
        public List<TransactionMsg> Transactions { get; set; }

        public TxsMsg()
        {
            this.Transactions = new List<TransactionMsg>();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];
            this.Transactions.Clear();

            Array.Copy(bytes, index, countBytes, 0, countBytes.Length);
            index += 4;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            int count = BitConverter.ToInt32(countBytes, 0);

            var txIndex = 0;
            while (txIndex < count)
            {
                var transactionMsg = new TransactionMsg();
                transactionMsg.Deserialize(bytes, ref index);
                this.Transactions.Add(transactionMsg);

                txIndex++;
            }
        }

        public override byte[] Serialize()
        {
            var countBytes = BitConverter.GetBytes(this.Count);
            var data = new List<byte>();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            data.AddRange(countBytes);


            foreach (var tx in Transactions)
            {
                data.AddRange(tx.Serialize());
            }

            return data.ToArray();
        }
    }
}
