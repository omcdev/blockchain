


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class GetHeadersMsg : BasePayload
    {
        public List<long> Heights { get; set; }
        public int Count
        {
            get { return this.Heights.Count; }
        }

        public GetHeadersMsg()
        {
            this.Heights = new List<long>();
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];
            this.Heights.Clear();

            Array.Copy(bytes, index, countBytes, 0, countBytes.Length);
            index += countBytes.Length;

            while (index < bytes.Length)
            {
                var heightBytes = new byte[8];
                Array.Copy(bytes, index, heightBytes, 0, heightBytes.Length);
                index += heightBytes.Length;

                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(heightBytes);
                }

                this.Heights.Add(BitConverter.ToInt64(heightBytes, 0));
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


            foreach (var height in Heights)
            { 
                var heightBytes = BitConverter.GetBytes(height);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(heightBytes);
                }

                data.AddRange(heightBytes);
            }

            return data.ToArray();
        }
    }
}
