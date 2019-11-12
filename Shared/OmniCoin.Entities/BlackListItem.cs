


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class BlackListItem
    {
        public long Id { get; set; }

        public string Address { get; set; }

        public long Timestamp { get; set; }

        public long? Expired { get; set; }
    }
}
