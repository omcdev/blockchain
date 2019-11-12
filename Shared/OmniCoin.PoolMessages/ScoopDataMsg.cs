using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class ScoopDataMsg : BasePayload
    {
        public long BlockHeight { get; set; }
        public string WalletAddress { get; set; }
        public long Nonce { get; set; }
        public int ScoopNumber { get; set; }
        public byte[] Target { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var blockHeightBytes = new byte[8];
            var walletAddressBytes = new byte[28];
            var nonceBytes = new byte[8];
            var scoopNumberBytes = new byte[4];
            var targetBytes = new byte[32];

            Array.Copy(bytes, index, blockHeightBytes, 0, blockHeightBytes.Length);
            index += blockHeightBytes.Length;

            Array.Copy(bytes, index, walletAddressBytes, 0, walletAddressBytes.Length);
            index += walletAddressBytes.Length;

            Array.Copy(bytes, index, nonceBytes, 0, nonceBytes.Length);
            index += nonceBytes.Length;

            Array.Copy(bytes, index, scoopNumberBytes, 0, scoopNumberBytes.Length);
            index += scoopNumberBytes.Length;

            Array.Copy(bytes, index, targetBytes, 0, targetBytes.Length);
            index += targetBytes.Length;

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(scoopNumberBytes);
            }

            this.BlockHeight = BitConverter.ToInt64(blockHeightBytes, 0);
            this.WalletAddress = Base58.Encode(walletAddressBytes);
            this.Nonce = BitConverter.ToInt64(nonceBytes, 0);
            this.ScoopNumber = BitConverter.ToInt32(scoopNumberBytes, 0);
            this.Target = targetBytes;
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            var blockHeightBytes = new byte[8];
            var walletAddressBytes = new byte[28];
            var nonceBytes = new byte[8];
            var scoopNumberBytes = new byte[4];
            var targetBytes = new byte[32];

            blockHeightBytes = BitConverter.GetBytes(this.BlockHeight);
            walletAddressBytes = Base58.Decode(this.WalletAddress);
            nonceBytes = BitConverter.GetBytes(this.Nonce);
            scoopNumberBytes = BitConverter.GetBytes(this.ScoopNumber);
            targetBytes = this.Target;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(blockHeightBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(scoopNumberBytes);
            }

            data.AddRange(blockHeightBytes);
            data.AddRange(walletAddressBytes);
            data.AddRange(nonceBytes);
            data.AddRange(scoopNumberBytes);
            data.AddRange(targetBytes);

            return data.ToArray();
        }
    }
}
