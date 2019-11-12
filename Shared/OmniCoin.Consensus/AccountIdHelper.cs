


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OmniCoin.Consensus
{
    public class AccountIdHelper
    {
        private const int testnetPrefix = 0x4E67B827;
        private const int mainnetPrefix = 0x4E67B816;
        private const int prefixLen = 4;

        public static string CreateAccountAddress(byte[] publicKey)
        {
            var pubkHash = HashHelper.Hash160(publicKey);

            return CreateAccountAddressByPublicKeyHash(pubkHash);
        }

        public static string CreateAccountAddressByPublicKeyHash(byte[] pubkHash)
        {            
            byte[] fullPrefix;

            if(GlobalParameters.IsTestnet)
            {
                fullPrefix = BitConverter.GetBytes(testnetPrefix);
            }
            else
            {
                fullPrefix = BitConverter.GetBytes(mainnetPrefix);
            }

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(fullPrefix);
            }

            var payload = new List<byte>();
            payload.AddRange(fullPrefix);
            payload.AddRange(pubkHash);

            var checksum = HashHelper.DoubleHash(payload.ToArray()).Take(4);
            payload.AddRange(checksum);

            return Base58.Encode(payload.ToArray());


        }

        public static byte[] GetPublicKeyHash(string accountAddress)
        {
            var bytes = Base58.Decode(accountAddress);
            var publicKeyHash = new byte[bytes.Length - (prefixLen + 4)];
            Array.Copy(bytes, prefixLen, publicKeyHash, 0, publicKeyHash.Length);

            return publicKeyHash;
        }

        public static bool AddressVerify(string accountAddress)
        {
            try
            {
                var bytes = Base58.Decode(accountAddress);

                if (bytes.Length != 28)
                {
                    return false;
                }

                var prefixBytes = new byte[4];
                Array.Copy(bytes, 0, prefixBytes, 0, 4);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(prefixBytes);
                }

                var prefixValue = BitConverter.ToInt32(prefixBytes, 0);

                if (GlobalParameters.IsTestnet)
                {
                    if (prefixValue != testnetPrefix)
                    {
                        return false;
                    }
                }
                else
                {
                    if (prefixValue != mainnetPrefix)
                    {
                        return false;
                    }
                }

                //var prefix = bytes[prefixLen];
                var checksum = new byte[4];
                var data = new byte[bytes.Length - 4];
                Array.Copy(bytes, 0, data, 0, bytes.Length - 4);
                Array.Copy(bytes, bytes.Length - checksum.Length, checksum, 0, checksum.Length);

                var newChecksum = HashHelper.DoubleHash(data).Take(4);
                return BitConverter.ToInt32(checksum, 0) == BitConverter.ToInt32(newChecksum.ToArray(), 0);
            }
            catch
            {
                return false;
            }
        }
    }
}
