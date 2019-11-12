


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class TxPoolMsg : BasePayload
    {
        public List<string> Hashes { get; set; }
        public int Count
        {
            get { return this.Hashes.Count; }
        }

        public TxPoolMsg()
        {
            this.Hashes = new List<string>();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];
            this.Hashes.Clear();

            index += 4;

            while(index < bytes.Length)
            {
                var hashBytes = new byte[32];
                Array.Copy(bytes, index, hashBytes, 0, hashBytes.Length);
                index += hashBytes.Length;

                this.Hashes.Add(Base16.Encode(hashBytes));
            }
        }

        public override byte[] Serialize()
        {
            var countBytes = BitConverter.GetBytes(this.Count);
            var data = new List<byte>();

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            data.AddRange(countBytes);
            

            foreach(var hash in Hashes)
            {
                data.AddRange(Base16.Decode(hash));
            }

            return data.ToArray();
        }
    }
}
