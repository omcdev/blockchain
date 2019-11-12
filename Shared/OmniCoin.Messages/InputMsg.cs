


using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    [Serializable]
    public class InputMsg : BasePayload
    {
        public string OutputTransactionHash { get; set; }
        public int OutputIndex { get; set; }
        public int Size { get; set; }
        public string UnlockScript { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var txHashBytes = new byte[32];
            var outputIndexBytes = new byte[4];
            var sizeBytes = new byte[4];

            Array.Copy(bytes, index, txHashBytes, 0, txHashBytes.Length);
            index += txHashBytes.Length;
            this.OutputTransactionHash = Base16.Encode(txHashBytes);

            Array.Copy(bytes, index, outputIndexBytes, 0, outputIndexBytes.Length);
            index += outputIndexBytes.Length;

            Array.Copy(bytes, index, sizeBytes, 0, sizeBytes.Length);
            index += sizeBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(outputIndexBytes);
                Array.Reverse(sizeBytes);
            }

            this.OutputIndex = BitConverter.ToInt32(outputIndexBytes, 0);
            this.Size = BitConverter.ToInt32(sizeBytes, 0);
            var scriptBytes = new byte[Size];
            Array.Copy(bytes, index, scriptBytes, 0, this.Size);
            this.UnlockScript = Encoding.UTF8.GetString(scriptBytes);

            index += Size;
        }

        public override byte[] Serialize()
        {
            var bytes = new List<byte>();
            bytes.AddRange(Base16.Decode(OutputTransactionHash));

            var indexBytes = BitConverter.GetBytes(OutputIndex);
            var sizeBytes = BitConverter.GetBytes(Size);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
                Array.Reverse(sizeBytes);
            }

            bytes.AddRange(indexBytes);
            bytes.AddRange(sizeBytes);
            bytes.AddRange(Encoding.UTF8.GetBytes(UnlockScript));

            return bytes.ToArray();

        }
    }
}
