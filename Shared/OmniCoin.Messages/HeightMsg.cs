


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class HeightMsg : BasePayload
    {
        public long Height { get; set; }

        public long BlockTime { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var heightBytes = new byte[8];
            var timeBytes = new byte[8];
            Array.Copy(bytes, index, heightBytes, 0, heightBytes.Length);
            index += heightBytes.Length;

            Array.Copy(bytes, index, timeBytes, 0, timeBytes.Length);
            index += timeBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(heightBytes);
                Array.Reverse(timeBytes);
            }

            this.Height = BitConverter.ToInt64(heightBytes, 0);
            this.BlockTime = BitConverter.ToInt64(timeBytes, 0);
        }

        public override byte[] Serialize()
        {
            var heightBytes = BitConverter.GetBytes(this.Height);
            var timeBytes = BitConverter.GetBytes(this.BlockTime);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(heightBytes);
                Array.Reverse(timeBytes);
            }

            var data = new List<byte>();
            data.AddRange(heightBytes);
            data.AddRange(timeBytes);

            return data.ToArray();
        }
    }
}
