


using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    [Serializable]
    public class OutputMsg : BasePayload
    {
        public int Index { get; set; }
        public long Amount { get; set; }
        public int Size { get; set; }
        public string LockScript { get; set; }

        private string GetHash()
        {
            var bytes = new List<byte>();
            var indexBytes = BitConverter.GetBytes(Index);
            var amountBytes = BitConverter.GetBytes(Amount);
            var sizeBytes = BitConverter.GetBytes(Size);
            var scriptBytes = Encoding.UTF8.GetBytes(LockScript);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
                Array.Reverse(amountBytes);
                Array.Reverse(sizeBytes);
            }

            bytes.AddRange(indexBytes);
            bytes.AddRange(amountBytes);
            bytes.AddRange(sizeBytes);
            bytes.AddRange(scriptBytes);

            return Base16.Encode(
                HashHelper.Hash(
                    bytes.ToArray()
                ));
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var indexBytes = new byte[4];
            var amountBytes = new byte[8];
            var sizeBytes = new byte[4];

            Array.Copy(bytes, index, indexBytes, 0, indexBytes.Length);
            index += indexBytes.Length;

            Array.Copy(bytes, index, amountBytes, 0, amountBytes.Length);
            index += amountBytes.Length;

            Array.Copy(bytes, index, sizeBytes, 0, sizeBytes.Length);
            index += sizeBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
                Array.Reverse(amountBytes);
                Array.Reverse(sizeBytes);
            }

            this.Index = BitConverter.ToInt32(indexBytes, 0);
            this.Amount = BitConverter.ToInt64(amountBytes, 0);
            this.Size = BitConverter.ToInt32(sizeBytes, 0);

            var scriptBytes = new byte[Size];
            Array.Copy(bytes, index, scriptBytes, 0, this.Size);
            this.LockScript = Encoding.UTF8.GetString(scriptBytes);

            index += Size;
        }

        public override byte[] Serialize()
        {
            var bytes = new List<byte>();
            var indexBytes = BitConverter.GetBytes(Index);
            var amountBytes = BitConverter.GetBytes(Amount);
            var sizeBytes = BitConverter.GetBytes(Size);
            var scriptBytes = Encoding.UTF8.GetBytes(LockScript);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
                Array.Reverse(amountBytes);
                Array.Reverse(sizeBytes);
            }

            bytes.AddRange(indexBytes);
            bytes.AddRange(amountBytes);
            bytes.AddRange(sizeBytes);
            bytes.AddRange(scriptBytes);

            return bytes.ToArray();
        }
    }
}
