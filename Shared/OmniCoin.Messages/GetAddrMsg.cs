


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class GetAddrMsg : BasePayload
    {
        public int Count { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];

            Array.Copy(bytes, index, countBytes, 0, countBytes.Length);
            index += countBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            this.Count = BitConverter.ToInt32(countBytes, 0);
        }

        public override byte[] Serialize()
        {
            var countBytes = BitConverter.GetBytes(this.Count);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            return countBytes;
        }
    }
}
