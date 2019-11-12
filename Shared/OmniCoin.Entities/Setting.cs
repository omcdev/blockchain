


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Entities
{
    public class Setting
    {
        public long Confirmations { get; set; }
        public long FeePerKB { get; set; }
        public bool Encrypt { get; set; }
        public string PassCiphertext { get; set; }
    }
}
