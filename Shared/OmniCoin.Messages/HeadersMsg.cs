


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class HeadersMsg : BasePayload
    {
        public int Count
        {
            get
            {
                return this.Headers.Count;
            }
        }
        public List<BlockHeaderMsg> Headers { get; set; }

        public HeadersMsg()
        {
            this.Headers = new List<BlockHeaderMsg>();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];
            this.Headers.Clear();

            Array.Copy(bytes, index, countBytes, 0, countBytes.Length);
            index += 4;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
            }

            int count = BitConverter.ToInt32(countBytes, 0);

            var headerIndex = 0;
            while (headerIndex < count)
            {
                var headerMsg = new BlockHeaderMsg();
                headerMsg.Deserialize(bytes, ref index);
                this.Headers.Add(headerMsg);

                headerIndex++;
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


            foreach (var header in Headers)
            {
                data.AddRange(header.Serialize());
            }

            return data.ToArray();
        }
    }
}
