


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class ListBannedOM
    {
        public string address { get; set; }
        public long banned_until { get; set; }
        public long banned_created { get; set; }
        public long banned_reason { get; set; }
    }
}
