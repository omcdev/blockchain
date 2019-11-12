


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO
{
    public class ValidateAddressOM
    {
        public bool isValid { get; set; }
        public string address { get; set; }
        public string scriptPubKey { get; set; }
        public bool isMine { get; set; }
        public bool isWatchOnly { get; set; }
        public bool isScript { get; set; }
        public string script { get; set; }
        public string hex { get; set; }
        public string[] addresses { get; set; }
        public int sigrequired { get; set; }
        public string pubKey { get; set; }
        public bool isCompressed { get; set; }
        public string account { get; set; }
        public string hdKeyPath { get; set; }
        public string hdMasterKeyId { get; set; }
    }
}
