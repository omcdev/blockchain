using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class NonceDataMsg : BasePayload
    {
        public long MaxNonce { get; set; }
        public byte[] ScoopData { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var maxNonceBytes = new byte[8];
            var scoopDataBytes = new byte[64];

            Array.Copy(bytes, index, maxNonceBytes, 0, maxNonceBytes.Length);
            index += maxNonceBytes.Length;

            Array.Copy(bytes, index, scoopDataBytes, 0, scoopDataBytes.Length);
            index += scoopDataBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(maxNonceBytes);
            }

            this.MaxNonce = BitConverter.ToInt64(maxNonceBytes, 0);
            this.ScoopData = scoopDataBytes;
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            byte[] maxNonceBytes = BitConverter.GetBytes(MaxNonce);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(maxNonceBytes);
            }

            data.AddRange(maxNonceBytes);
            data.AddRange(ScoopData);

            return data.ToArray();
        }
    }
}
