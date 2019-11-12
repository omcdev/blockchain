


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class NewTxMsg : BasePayload
    {
        public string Hash { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var hashBytes = new byte[32];
            Array.Copy(bytes, index, hashBytes, 0, hashBytes.Length);
            index += hashBytes.Length;

            this.Hash = Base16.Encode(hashBytes);
        }

        public override byte[] Serialize()
        {
            return Base16.Decode(Hash);
        }
    }
}
