using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class StopMsg : BasePayload
    {
        public bool Result { get; set; }
        public long BlockHeight { get; set; }
        public long StartTime { get; set; }
        public long StopTime { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            byte resultByte;
            var blockHeightBytes = new byte[8];
            var startTimeBytes = new byte[8];
            var stopTimeBytes = new byte[8];

            resultByte = bytes[index];
            index += 1;

            Array.Copy(bytes, index, blockHeightBytes, 0, blockHeightBytes.Length);
            index += blockHeightBytes.Length;

            Array.Copy(bytes, index, startTimeBytes, 0, startTimeBytes.Length);
            index += startTimeBytes.Length;

            Array.Copy(bytes, index, stopTimeBytes, 0, stopTimeBytes.Length);
            index += stopTimeBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(startTimeBytes);
                Array.Reverse(stopTimeBytes);
            }

            this.Result = resultByte == 1;
            this.BlockHeight = BitConverter.ToInt64(blockHeightBytes, 0);
            this.StartTime = BitConverter.ToInt64(startTimeBytes, 0);
            this.StopTime = BitConverter.ToInt64(stopTimeBytes, 0);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            byte resultByte;
            var blockHeightBytes = new byte[8];
            var startTimeBytes = new byte[8];
            var stopTimeBytes = new byte[8];

            resultByte = this.Result ? (byte)1 : (byte)0;
            blockHeightBytes = BitConverter.GetBytes(this.BlockHeight);
            startTimeBytes = BitConverter.GetBytes(this.StartTime);
            stopTimeBytes = BitConverter.GetBytes(this.StopTime);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(startTimeBytes);
                Array.Reverse(stopTimeBytes);
            }

            data.Add(resultByte);
            data.AddRange(blockHeightBytes);
            data.AddRange(startTimeBytes);
            data.AddRange(stopTimeBytes);

            return data.ToArray();
        }
    }
}
