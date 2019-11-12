using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class P2PPacket
    {
        public string Prefix { get; set; }
        public string Id { get; set; }
        public int Count { get; set; }
        public int Index { get; set; }
        public byte[] Data { get; set; }

        public static byte[] DefaultPacketPrefixBytes
        {
            get
            {
                var prefix = Convert.ToInt32(Resource.PacketPrefix, 16);
                var prefixBytes = BitConverter.GetBytes(prefix);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(prefixBytes);
                }

                return prefixBytes;
            }
        }
        public static bool BytesEquals(byte[] bytes1, byte[] bytes2)
        {
            if (bytes1 == null || bytes2 == null)
            {
                return false;
            }

            if (bytes1.Length != bytes2.Length)
            {
                return false;
            }

            for (int i = 0; i < bytes1.Length; i++)
            {
                if (bytes1[i] != bytes2[i])
                {
                    return false;
                }
            }

            return true;
        }
        public static P2PPacket Deserialize(byte[] bytes)
        {
            var packet = new P2PPacket();
            var prefixBytes = new byte[4];
            var idBytes = new byte[4];
            var countBytes = new byte[4];
            var indexBytes = new byte[4];

            var index = 0;
            Array.Copy(bytes, index, prefixBytes, 0, prefixBytes.Length);
            index += prefixBytes.Length;

            Array.Copy(bytes, index, idBytes, 0, idBytes.Length);
            index += idBytes.Length;

            Array.Copy(bytes, index, indexBytes, 0, indexBytes.Length);
            index += indexBytes.Length;

            Array.Copy(bytes, index, countBytes, 0, countBytes.Length);
            index += countBytes.Length;

            var data = new byte[bytes.Length - index];
            Array.Copy(bytes, index, data, 0, data.Length);
            index += data.Length;

            if(BitConverter.IsLittleEndian)
            {
                //Array.Reverse(prefixBytes);
                Array.Reverse(countBytes);
                Array.Reverse(indexBytes);
            }

            packet.Prefix = Base16.Encode(prefixBytes);
            packet.Id = Base16.Encode(idBytes);
            packet.Count = BitConverter.ToInt32(countBytes, 0);
            packet.Index = BitConverter.ToInt32(indexBytes, 0);
            packet.Data = data;

            return packet;
        }
        public byte[] Serialize()
        {
            var data = new List<byte>();
            var prefixBytes = Base16.Decode(this.Prefix);
            var idBytes = Base16.Decode(this.Id);
            var indexBytes = BitConverter.GetBytes(this.Index);
            var countBytes = BitConverter.GetBytes(this.Count);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(countBytes);
                Array.Reverse(indexBytes);
            }
            
            data.AddRange(prefixBytes);
            data.AddRange(idBytes);
            data.AddRange(indexBytes);
            data.AddRange(countBytes);
            data.AddRange(this.Data);

            return data.ToArray();
        }

        public string GetCommandName()
        {
            if(this.Index != 0)
            {
                return null;
            }
            else
            {
                return P2PCommand.GetCommandName(this.Data);
            }
        }

    }
}
