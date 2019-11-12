


using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class Base16
    {
        public static string Encode(byte[] bytes)
        {
            return SimpleBase.Base16.EncodeUpper(bytes);
        }

        public static byte[] Decode(string text)
        {
            return SimpleBase.Base16.Decode(text);
        }

        public static object Encode(object p)
        {
            throw new NotImplementedException();
        }
    }
}
