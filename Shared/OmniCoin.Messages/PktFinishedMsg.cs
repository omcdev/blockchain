using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class PktFinishedMsg : BasePayload
    {
        public string Id { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var idBytes = new byte[4];
            Array.Copy(bytes, index, idBytes, 0, idBytes.Length);
            index += idBytes.Length;

            this.Id = Base16.Encode(idBytes);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            data.AddRange(Base16.Decode(Id));

            return data.ToArray();
        }
    }
}
