


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class RejectMsg : BasePayload
    {
        public int ReasonCode { get; set; }
        public string ReasonMsg { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var codeBytes = new byte[4];
            Array.Copy(bytes, index, codeBytes, 0, 4);
            index += codeBytes.Length;

            var msgBytes = new byte[bytes.Length - index];
            Array.Copy(bytes, index, msgBytes, 0, msgBytes.Length);
            index += msgBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(codeBytes);
            }

            this.ReasonCode = BitConverter.ToInt32(codeBytes, 0);
            this.ReasonMsg = Encoding.UTF8.GetString(msgBytes);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();

            var codeBytes = BitConverter.GetBytes(this.ReasonCode);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(codeBytes);
            }

            data.AddRange(codeBytes);

            if(this.ReasonMsg != null)
            {
                data.AddRange(Encoding.UTF8.GetBytes(this.ReasonMsg));
            }

            return data.ToArray();
        }
    }
}
