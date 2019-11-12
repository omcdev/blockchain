

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class Sha3Helper
    {
        public static byte[] Hash(byte[] data)
        {
            var sha3 = SHA3.Net.Sha3.Sha3256();
            return sha3.ComputeHash(data);
        }
    }
}
