using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class PktLostMsg : BasePayload
    {
        public string Id { get; set; }
        public int Index { get; set; }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            var indexBytes = BitConverter.GetBytes(Index);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            data.AddRange(Base16.Decode(Id));
            data.AddRange(indexBytes);

            return data.ToArray();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var idBytes = new byte[4];
            var indexBytes = new byte[4];

            Array.Copy(bytes, index, idBytes, 0, idBytes.Length);
            index += idBytes.Length;

            Array.Copy(bytes, index, indexBytes, 0, indexBytes.Length);
            index += indexBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            this.Id = Base16.Encode(idBytes);
            this.Index = BitConverter.ToInt32(indexBytes, 0);
        }
    }
}
