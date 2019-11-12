using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO.Transaction
{
    public class InputsOM
    {
        public long Id { get; set; }
        public string TransactionHash { get; set; }
        public string OutputTransactionHash { get; set; }
        public int OutputIndex { get; set; }
        public string BlockHash { get; set; }

        public string Txid { get; set; }

        public int Vout { get; set; }

        public int Size { get; set; }

        public long Amount { get; set; }

        public string UnlockScript { get; set; }

        public string AccountId { get; set; }

        public bool IsDiscarded { get; set; }
    }
}
