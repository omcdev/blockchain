using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class LoginMsg : BasePayload
    {
        public string WalletAddress { get; set; }
        public EnumMinerType MinerType { get; set; }
        public string SerialNo { get; set; }

        public LoginMsg()
        {
            MinerType = EnumMinerType.POS;
        }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var addressBytes = new byte[28];
            byte minerTypeByte;

            Array.Copy(bytes, index, addressBytes, 0, addressBytes.Length);
            index += addressBytes.Length;
            minerTypeByte = bytes[index];
            index += 1;

            var serialNoBytes = new byte[bytes.Length - index];
            Array.Copy(bytes, index, serialNoBytes, 0, serialNoBytes.Length);
            index += serialNoBytes.Length;

            this.WalletAddress = Base58.Encode(addressBytes);
            this.MinerType = (EnumMinerType)minerTypeByte;
            this.SerialNo = Encoding.UTF8.GetString(serialNoBytes);
        }

        public override byte[] Serialize()
        {
            var addressBytes = Base58.Decode(WalletAddress);
            var minerTypeBytes = (byte)MinerType;
            var serialNoBytes = Encoding.UTF8.GetBytes(SerialNo);

            var data = new List<byte>();
            data.AddRange(addressBytes);
            data.Add(minerTypeBytes);
            data.AddRange(serialNoBytes);

            return data.ToArray();
        }
    }
}
