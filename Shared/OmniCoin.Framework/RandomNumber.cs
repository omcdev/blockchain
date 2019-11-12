


using NSec.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class RandomNumber
    {
        public static byte[] Generate(int size)
        {
            return RandomGenerator.Default.GenerateBytes(size);
        }
    }
}
