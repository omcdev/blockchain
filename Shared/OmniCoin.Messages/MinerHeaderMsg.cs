


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class MiningMsg : BasePayload
    {
        public string Name { get; set; }
        public string PublicKey { get; set; }
        public string Signature { get; set; }
        private int nameLength { get; set; }
        private int publicKeyLength { get; set; }

        public override void Deserialize(byte[] bytes, ref int index)
        {
            var namelengthBytes = new byte[4];
            Array.Copy(bytes, index, namelengthBytes, 0, namelengthBytes.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(namelengthBytes);
            }

            nameLength = BitConverter.ToInt32(namelengthBytes, 0);
            index += 4;

            var nameBytes = new byte[nameLength];
            Array.Copy(bytes, index, nameBytes, 0, nameBytes.Length);
            Name = Encoding.UTF8.GetString(nameBytes);
            index += nameBytes.Length;


            var publicKeyLengthBytes = new byte[4];
            Array.Copy(bytes, index, publicKeyLengthBytes, 0, publicKeyLengthBytes.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(publicKeyLengthBytes);
            }

            publicKeyLength = BitConverter.ToInt32(publicKeyLengthBytes, 0);
            index += 4;

            var publicKeyBytes = new byte[publicKeyLength];
            Array.Copy(bytes, index, publicKeyBytes, 0, publicKeyBytes.Length);
            PublicKey = Base16.Encode(publicKeyBytes);
            index += publicKeyBytes.Length;


            var signatureLengthBytes = new byte[4];
            Array.Copy(bytes, index, signatureLengthBytes, 0, signatureLengthBytes.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(signatureLengthBytes);
            }

            var signatureLength = BitConverter.ToInt32(signatureLengthBytes, 0);
            index += 4;

            var signatureBytes = new byte[signatureLength];
            Array.Copy(bytes, index, signatureBytes, 0, signatureBytes.Length);
            index += signatureBytes.Length;
            Signature = Base16.Encode(signatureBytes);
        }

        public override byte[] Serialize()
        {
            var data = new List<byte>();
            var nameBytes = Encoding.UTF8.GetBytes(Name);
            var publicKeyBytes = Base16.Decode(PublicKey);
            var signatureBytes = Base16.Decode(Signature);
            var signatureLengthBytes = BitConverter.GetBytes(signatureBytes.Length);
            var nameLengthBytes = BitConverter.GetBytes(nameBytes.Length);
            var publicKeyLengthBytes = BitConverter.GetBytes(publicKeyBytes.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(nameLengthBytes);
                Array.Reverse(signatureLengthBytes);
                Array.Reverse(publicKeyLengthBytes);
            }

            data.AddRange(nameLengthBytes);
            data.AddRange(nameBytes);
            data.AddRange(publicKeyLengthBytes);
            data.AddRange(publicKeyBytes);
            data.AddRange(signatureLengthBytes);
            data.AddRange(signatureBytes);
            return data.ToArray();
        }
    }
}
