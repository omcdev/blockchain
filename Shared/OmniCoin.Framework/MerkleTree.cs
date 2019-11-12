


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class MerkleTree
    {
        public static byte[] Hash(List<byte[]> bytes)
        {
            if (bytes == null)
                return null;
            if (bytes.Count == 1)
                return bytes[0];

            List<byte[]> items = new List<byte[]>();
            int index = 0;
            var len = bytes.Count;
            while (index < len)
            {
                var bytes1 = bytes[index];
                var bytes2 = new byte[bytes1.Length];
                index++;
                
                if (len > index)
                { 
                    bytes2 = bytes[index];
                }
                else
                { 
                    Array.Copy(bytes1, 0, bytes2, 0, bytes2.Length);
                }
                items.Add(Hash2(bytes1, bytes2));
                index++;
            }
            return Hash(items);
        }

        private static byte[] Hash2(byte[] bytes1, byte[] bytes2)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(bytes1);
            bytes.AddRange(bytes2);
            return Sha3Helper.Hash(bytes.ToArray());
        }

    }
}
