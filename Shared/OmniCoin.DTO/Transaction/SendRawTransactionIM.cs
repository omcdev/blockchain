using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO.Transaction
{
    public class SendRawTransactionInputsIM
    {
        public string TxId { get; set; }

        public int Vout { get; set; }
    }

    public class SendRawTransactionOutputsIM
    {
        public string Address { get; set; }

        public long Amount { get; set; }
    }
}
