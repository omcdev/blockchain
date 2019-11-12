using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class PageUnspent
    {
        public List<OutputConfirmInfo> Outputs { get; set; }

        public long Count { get; set; }
    }
}
