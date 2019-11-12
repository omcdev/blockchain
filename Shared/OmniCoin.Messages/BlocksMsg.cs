


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class BlocksMsg : BasePayload
    {
        public int Count
        {
            get
            {
                return this.Blocks.Count;
            }
        }
        public List<BlockMsg> Blocks { get; set; }

        public BlocksMsg()
        {
            this.Blocks = new List<BlockMsg>();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];
            this.Blocks.Clear();

            Array.Copy(bytes, index, countBytes, 0, countBytes.Length);
            index += 4;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            int count = BitConverter.ToInt32(countBytes, 0);

            var blockIndex = 0;
            while (blockIndex < count)
            {
                var blockMsg = new BlockMsg();
                blockMsg.Deserialize(bytes, ref index);
                this.Blocks.Add(blockMsg);

                blockIndex++;
            }
        }

        public override byte[] Serialize()
        {
            var countBytes = BitConverter.GetBytes(this.Count);
            var data = new List<byte>();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            data.AddRange(countBytes);


            foreach (var block in Blocks)
            {
                data.AddRange(block.Serialize());
            }

            return data.ToArray();
        }
    }
}
