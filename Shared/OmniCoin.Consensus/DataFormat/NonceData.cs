

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Consensus
{
    public class NonceData
    {
        public NonceData()
        {
            DataList = new List<ScoopData>();
        }
        public long Nonce { get; set; }
        public List<ScoopData> DataList { get; set; }
    }
}
