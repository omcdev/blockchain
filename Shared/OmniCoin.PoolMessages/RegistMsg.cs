


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class RegistMsg : BasePayload
    {
        public string WalletAddress;
        public string SerialNo;
        private int SerialNoLen;
        public string Name;

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var addressBytes = new byte[28];
            Array.Copy(bytes, index, addressBytes, 0, addressBytes.Length);
            index += addressBytes.Length;

            var serialNoLenBytes = new byte[4];
            Array.Copy(bytes, index, serialNoLenBytes, 0, serialNoLenBytes.Length);
            SerialNoLen = BitConverter.ToInt32(serialNoLenBytes, 0);
            index += serialNoLenBytes.Length;

            var serialNoBytes = new byte[SerialNoLen];
            Array.Copy(bytes, index, serialNoBytes, 0, serialNoBytes.Length);
            index += serialNoBytes.Length;

            var nameBytes = new byte[bytes.Length - index - 1];
            Array.Copy(bytes, index, nameBytes, 0, nameBytes.Length);
            index += nameBytes.Length;

            WalletAddress = Base58.Encode(addressBytes);
            SerialNo = Encoding.UTF8.GetString(serialNoBytes);
            Name = Encoding.UTF8.GetString(nameBytes);
        }

        public override byte[] Serialize()
        {
            byte[] addressBytes = Base58.Decode(WalletAddress);
            byte[] serialNoBytes = Encoding.UTF8.GetBytes(SerialNo);
            byte[] serialNoLenBytes = BitConverter.GetBytes(serialNoBytes.Length);
            byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
            
            List<byte> data = new List<byte>();

            data.AddRange(addressBytes);
            data.AddRange(serialNoLenBytes);
            data.AddRange(serialNoBytes);
            data.AddRange(nameBytes);
            return data.ToArray();
        }
    }
}
