using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class RewardMsg : BasePayload
    {
        public long BlockHeight { get; set; }
        public string WalletAddress { get; set; }
        public long Nonce { get; set; }
        public long Timestamp { get; set; }
        public long TotalHashes { get; set; }
        public long MinerHashes { get; set; }
        public long TotalReward { get; set; }
        public long MinerReward { get; set; }


        public override void Deserialize(byte[] bytes, ref int index)
        {
            var blockHeightBytes = new byte[8];
            var walletAddressBytes = new byte[28];
            var nonceBytes = new byte[8];
            var timeStampBytes = new byte[8];
            var totalHashesBytes = new byte[8];
            var minerHashesBytes = new byte[8];
            var totalRewardBytes = new byte[8];
            var minerRewardBytes = new byte[8];

            Array.Copy(bytes, index, blockHeightBytes, 0, blockHeightBytes.Length);
            index += blockHeightBytes.Length;

            Array.Copy(bytes, index, walletAddressBytes, 0, walletAddressBytes.Length);
            index += walletAddressBytes.Length;

            Array.Copy(bytes, index, nonceBytes, 0, nonceBytes.Length);
            index += nonceBytes.Length;

            Array.Copy(bytes, index, timeStampBytes, 0, timeStampBytes.Length);
            index += timeStampBytes.Length;

            Array.Copy(bytes, index, totalHashesBytes, 0, totalHashesBytes.Length);
            index += totalHashesBytes.Length;

            Array.Copy(bytes, index, minerHashesBytes, 0, minerHashesBytes.Length);
            index += minerHashesBytes.Length;

            Array.Copy(bytes, index, totalRewardBytes, 0, totalRewardBytes.Length);
            index += totalRewardBytes.Length;

            Array.Copy(bytes, index, minerRewardBytes, 0, minerRewardBytes.Length);
            index += minerRewardBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(timeStampBytes);
                Array.Reverse(totalHashesBytes);
                Array.Reverse(minerHashesBytes);
                Array.Reverse(totalRewardBytes);
                Array.Reverse(minerRewardBytes);
            }

            this.BlockHeight = BitConverter.ToInt64(blockHeightBytes, 0);
            this.WalletAddress = Base58.Encode(walletAddressBytes);
            this.Nonce = BitConverter.ToInt64(nonceBytes, 0);
            this.Timestamp = BitConverter.ToInt64(timeStampBytes, 0);
            this.TotalHashes = BitConverter.ToInt64(totalHashesBytes, 0);
            this.MinerHashes = BitConverter.ToInt64(minerHashesBytes, 0);
            this.TotalReward = BitConverter.ToInt64(totalRewardBytes, 0);
            this.MinerReward = BitConverter.ToInt64(minerRewardBytes, 0);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            var blockHeightBytes = new byte[8];
            var walletAddressBytes = new byte[28];
            var nonceBytes = new byte[8];
            var timeStampBytes = new byte[8];
            var totalHashesBytes = new byte[8];
            var minerHashesBytes = new byte[8];
            var totalRewardBytes = new byte[8];
            var minerRewardBytes = new byte[8];

            blockHeightBytes = BitConverter.GetBytes(this.BlockHeight);
            walletAddressBytes = Base58.Decode(this.WalletAddress);
            nonceBytes = BitConverter.GetBytes(this.Nonce);
            timeStampBytes = BitConverter.GetBytes(this.Timestamp);
            totalHashesBytes = BitConverter.GetBytes(this.TotalHashes);
            minerHashesBytes = BitConverter.GetBytes(this.MinerHashes);
            totalRewardBytes = BitConverter.GetBytes(TotalReward);
            minerRewardBytes = BitConverter.GetBytes(this.MinerReward);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(timeStampBytes);
                Array.Reverse(totalHashesBytes);
                Array.Reverse(minerHashesBytes);
                Array.Reverse(totalRewardBytes);
                Array.Reverse(minerRewardBytes);
            }

            data.AddRange(blockHeightBytes);
            data.AddRange(walletAddressBytes);
            data.AddRange(nonceBytes);
            data.AddRange(timeStampBytes);
            data.AddRange(totalHashesBytes);
            data.AddRange(minerHashesBytes);
            data.AddRange(totalRewardBytes);
            data.AddRange(minerRewardBytes);

            return data.ToArray();
        }
    }
}
