


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class Base58
    {
        public static string Encode(byte[] bytes)
        {
            return SimpleBase.Base58.Bitcoin.Encode(bytes);
        }

        public static byte[] Decode(string text)
        {
            return SimpleBase.Base58.Bitcoin.Decode(text); 
        }
    }
}
