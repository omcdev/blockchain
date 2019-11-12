using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO.Utxo
{
    public class ListPageUnspentOM
    {
        public List<ListUnspentOM> UnspentOMList { get; set; }

        public long Count { get; set; }
    }
}
