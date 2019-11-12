


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class RegistResultMsg : BasePayload
    {
        public bool Result { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            if (bytes[index] == 0x1)
            {
                Result = true;
            }
            else
            {
                Result = false;
            }

            index += 1;
        }

        public override byte[] Serialize()
        {
            byte resultByte = Result ? (byte)0x01 : (byte)0x00;
            return new byte[] { resultByte };
        }
    }
}
