



using System.Security.Cryptography;

namespace OmniCoin.MiningPool.Award.AddressValidate
{
    public class HashHelper
    {
        public static byte[] Hash(byte[] bytes)
        {
            SHA256 sha256 = SHA256Managed.Create();
            return sha256.ComputeHash(bytes);
        }

        public static byte[] DoubleHash(byte[] bytes)
        {
            return Hash(
                    Hash(bytes)
                );
        }
        public static byte[] EmptyHash()
        {
            return new byte[32];
        }
    }
}
