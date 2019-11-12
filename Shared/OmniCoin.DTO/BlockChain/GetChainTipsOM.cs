


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class GetChainTipsOM
    {
        public long height { get; set; }
        public string hash { get; set; }
        public long branchLen { get; set; }
        public string status { get; set; }
    }
}
