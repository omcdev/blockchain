


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class VersionMsg : BasePayload
    {
        public int Version { get; set; }
        public long Timestamp { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var verBytes = new byte[4];
            var timestampBytes = new byte[8];

            Array.Copy(bytes, index, verBytes, 0, verBytes.Length);
            index += verBytes.Length;

            Array.Copy(bytes, index, timestampBytes, 0, timestampBytes.Length);
            index += timestampBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(verBytes);
                Array.Reverse(timestampBytes);
            }

            this.Version = BitConverter.ToInt32(verBytes, 0);
            this.Timestamp = BitConverter.ToInt64(timestampBytes, 0);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();

            var verBytes = BitConverter.GetBytes(this.Version);
            var timestampBytes = BitConverter.GetBytes(this.Timestamp);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(verBytes);
                Array.Reverse(timestampBytes);
            }

            data.AddRange(verBytes);
            data.AddRange(timestampBytes);

            return data.ToArray();
        }
    }
}
