


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class NewBlockMsg : BasePayload
    {
        public BlockHeaderMsg Header { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            this.Header = new BlockHeaderMsg();
            this.Header.Deserialize(bytes, ref index);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(Header.Serialize());

            return data.ToArray();
        }
    }
}
