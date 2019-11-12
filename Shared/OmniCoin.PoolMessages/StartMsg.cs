using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class StartMsg : BasePayload
    {
        public long BlockHeight { get; set; }
        public int ScoopNumber { get; set; }
        public long StartTime { get; set; }
        public byte[] GenHash { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var blockHeightBytes = new byte[8];
            var scoopNumberBytes = new byte[4];
            var startTimeBytes = new byte[8];
            var genHashBytes = new byte[32];

            Array.Copy(bytes, index, blockHeightBytes, 0, blockHeightBytes.Length);
            index += blockHeightBytes.Length;

            Array.Copy(bytes, index, scoopNumberBytes, 0, scoopNumberBytes.Length);
            index += scoopNumberBytes.Length;

            Array.Copy(bytes, index, startTimeBytes, 0, startTimeBytes.Length);
            index += startTimeBytes.Length;

            Array.Copy(bytes, index, genHashBytes, 0, genHashBytes.Length);
            index += genHashBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(scoopNumberBytes);
                Array.Reverse(startTimeBytes);
            }

            this.BlockHeight = BitConverter.ToInt64(blockHeightBytes, 0);
            this.ScoopNumber = BitConverter.ToInt32(scoopNumberBytes, 0);
            this.StartTime = BitConverter.ToInt64(startTimeBytes, 0);
            this.GenHash = genHashBytes;
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            var blockHeightBytes = new byte[8];
            var scoopNumberBytes = new byte[4];
            var startTimeBytes = new byte[8];
            var genHashBytes = new byte[32];

            blockHeightBytes = BitConverter.GetBytes(this.BlockHeight);
            scoopNumberBytes = BitConverter.GetBytes(this.ScoopNumber);
            startTimeBytes = BitConverter.GetBytes(this.StartTime);
            genHashBytes = this.GenHash;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(scoopNumberBytes);
                Array.Reverse(startTimeBytes);
            }

            data.AddRange(blockHeightBytes);
            data.AddRange(scoopNumberBytes);
            data.AddRange(startTimeBytes);
            data.AddRange(genHashBytes);

            return data.ToArray();
        }
    }
}
