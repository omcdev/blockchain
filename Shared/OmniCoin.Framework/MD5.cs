


using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OmniCoin.Framework
{
    public class MD5Helper
    {
        public static string EncryptTo32(string source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString("x2"));

            }
            return strbul.ToString().ToUpper();
        }

        public static string EncryptTo64(string source)
        {
            byte[] sor = Encoding.UTF8.GetBytes(source);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(sor);
            StringBuilder strbul = new StringBuilder(40);
            for (int i = 0; i < result.Length; i++)
            {
                strbul.Append(result[i].ToString("x4"));

            }
            return strbul.ToString().ToUpper();
        }
    }
}
