using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class MaxNonceMsg : BasePayload
    {
        public int RandomScoopNumber { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var numberBytes = new byte[4];

            Array.Copy(bytes, index, numberBytes, 0, numberBytes.Length);
            index += numberBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(numberBytes);
            }

            this.RandomScoopNumber = BitConverter.ToInt32(numberBytes, 0);
        }

        public override byte[] Serialize()
        {
            byte[] numberBytes = BitConverter.GetBytes(this.RandomScoopNumber);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(numberBytes);
            }

            return numberBytes;
        }
    }
}
