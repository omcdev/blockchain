


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class GetMiningPoolMsg : BasePayload
    {
        public GetMiningPoolMsg()
        {
            MinerInfos = new List<MiningMsg>();
        }

        public List<MiningMsg> MinerInfos { get; set; }

        public int Count
        {
            get { return this.MinerInfos.Count; }
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var countBytes = new byte[4];
            this.MinerInfos.Clear();

            index += 4;

            while (index < bytes.Length)
            {
                MiningMsg minerInfo = new MiningMsg();
                minerInfo.Deserialize(bytes, ref index);
                this.MinerInfos.Add(minerInfo);
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

            foreach (var minerInfo in MinerInfos)
            {
                var bytes = minerInfo.Serialize();
            }
            return data.ToArray();
        }
    }
}
