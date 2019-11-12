using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.PoolMessages
{
    public class PoolCommand
    {
        public byte[] Prefix { get; set; }
        public string CommandName { get; set; }
        public int Size { get; set; }
        public byte[] Payload { get; set; }
        public byte[] Checksum { get; set; }
        public byte[] Suffix { get; set; }

        public static byte[] DefaultPrefixBytes
        {
            get
            {
                var prefix = Convert.ToInt32(Resource.Prefix, 16);
                var prefixBytes = BitConverter.GetBytes(prefix);

                if(BitConverter.IsLittleEndian)
                {
                    Array.Reverse(prefixBytes);
                }

                return prefixBytes;
            }
        }

        public static byte[] DefaultSuffixBytes
        {
            get
            {
                var suffix = Convert.ToInt32(Resource.Suffix.Replace("0x", ""), 16);
                var suffixBytes = BitConverter.GetBytes(suffix);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(suffixBytes);
                }

                return suffixBytes;
            }
        }

        public static PoolCommand CreateCommand(string commandName, BasePayload payload)
        {
            var command = new PoolCommand();
            command.Prefix = DefaultPrefixBytes;
            command.Suffix = DefaultSuffixBytes;
            command.CommandName = commandName;

            if(payload != null)
            {
                var bytes = payload.Serialize();
                command.Payload = bytes;
                command.Size = bytes.Length;
                command.Checksum = GetChecksum(bytes);
            }
            else
            {
                var bytes = new byte[0];
                command.Payload = bytes;
                command.Size = bytes.Length;
                command.Checksum = GetChecksum(bytes);
            }

            return command;
        }

        public static PoolCommand ConvertBytesToMessage(byte[] bytes)
        {
            var prefixBytes = new byte[4];
            var suffixBytes = new byte[4];

            Array.Copy(bytes, 0, prefixBytes, 0, prefixBytes.Length);
            Array.Copy(bytes, bytes.Length - suffixBytes.Length, suffixBytes, 0, suffixBytes.Length);

            if (!BytesEquals(DefaultPrefixBytes, prefixBytes) || !BytesEquals(DefaultSuffixBytes, suffixBytes))
            {
                return null;
            }

            var commandBytes = new byte[12];
            var sizeBytes = new byte[4];

            Array.Copy(bytes, 4, commandBytes, 0, 12);
            Array.Copy(bytes, 16, sizeBytes, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sizeBytes);
            }

            var commandTextBytes = new List<byte>();
            foreach (var b in commandBytes)
            {
                if (b > 0x00)
                {
                    commandTextBytes.Add(b);
                }
                else
                {
                    break;
                }
            }

            var commandName = Encoding.UTF8.GetString(commandTextBytes.ToArray());
            int size = BitConverter.ToInt32(sizeBytes, 0);

            var payloadBytes = new byte[size];
            var checksumBytes = new byte[4];
            Array.Copy(bytes, 20, payloadBytes, 0, size);
            Array.Copy(bytes, 20 + size, checksumBytes, 0, 4);

            if (!BytesEquals(checksumBytes, GetChecksum(payloadBytes)))
            {
                return null;
            }


            var command = new PoolCommand();
            command.Prefix = prefixBytes;
            command.CommandName = commandName;
            command.Size = size;
            command.Payload = payloadBytes;
            command.Checksum = checksumBytes;
            command.Suffix = suffixBytes;

            return command;
        }

        public byte[] GetBytes()
        {
            var bytes = new List<byte>();
            var commandData = Encoding.UTF8.GetBytes(this.CommandName);
            var commandBytes = new byte[12];
            Array.Copy(commandData, 0, commandBytes, 0, commandData.Length);
            var sizeBytes = BitConverter.GetBytes(this.Size);

            if(BitConverter.IsLittleEndian)
            {
                //Array.Reverse(prefixBytes);
                Array.Reverse(sizeBytes);
                //Array.Reverse(suffixBytes);
            }

            bytes.AddRange(this.Prefix);
            bytes.AddRange(commandBytes);
            bytes.AddRange(sizeBytes);
            bytes.AddRange(this.Payload);
            bytes.AddRange(Checksum);
            bytes.AddRange(this.Suffix);

            return bytes.ToArray();
        }

        private static byte[] GetChecksum(byte[] bytes)
        {
            var hash = HashHelper.Hash(HashHelper.Hash(bytes));
            var checksum = new byte[4];
            Array.Copy(hash, 0, checksum, 0, 4);

            return checksum;
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
    }
}
