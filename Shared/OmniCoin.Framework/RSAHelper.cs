


using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using XC.Framework.Security.RSAUtil;

namespace OmniCoin.Framework
{
    public class RSAHelper
    {
        public static string Encrypt(string data, string publicKey)
        {
            RsaPkcs8Util rsa = new RsaPkcs8Util(Encoding.UTF8, publicKey);
            var result = rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
            return result;
        }

        public static string Decrypt(string utf8Data, string privateKey)
        {
            RsaPkcs8Util rsa = new RsaPkcs8Util(Encoding.UTF8, null, privateKey);
            var result = rsa.Encrypt(utf8Data, RSAEncryptionPadding.Pkcs1);
            return result;
        }
        
        public static void GenerateKeyFile(string content, string file)
        {
            FileStream stream = new FileStream(file, FileMode.OpenOrCreate);
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                bw.Write(bytes);
            }
            stream.Dispose();
            stream.Close();
        }

        public static string ReadPrivateKey(string file)
        {
            var result = "";
            FileStream stream = new FileStream(file, FileMode.OpenOrCreate);
            
            byte[] bytes = new byte[stream.Length]; 
            stream.Read(bytes,0, bytes.Length);
            result = Encoding.UTF8.GetString(bytes);
            stream.Dispose();
            stream.Close();
            return result;
        }
    }
}
