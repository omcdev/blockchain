


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Messages
{
    public class BlockHeaderMsg : BasePayload
    {
        public BlockHeaderMsg()
        {
            this.Version = Int32.Parse(Resource.MsgVersion);
        }

        public int Version { get; set; }
        public string Hash { get; set; }
        public long Height { get; set; }
        public string PreviousBlockHash { get; set; }
        public long Bits { get; set; }
        public long Nonce { get; set; }
        public long Timestamp { get; set; }
        public string GeneratorId { get; set; }
        public string PayloadHash { get; set; }
        public int BlockSigSize { get; set; }
        public string BlockSignature { get; set; }
        public int TotalTransaction { get; set; }

        public string GetHash()
        {
            //LogHelper.Info("PrevHash:" + PreviousBlockHash);
            //LogHelper.Info("Bits:" + Bits);
            //LogHelper.Info("Nonce:" + Nonce);
            //LogHelper.Info("Timestamp:" + Timestamp);
            //LogHelper.Info("GeneratorId:" + GeneratorId);
            //LogHelper.Info("PayloadHash:" + PayloadHash);
            //LogHelper.Info("BlockSignature:" + BlockSignature);

            var bytes = new List<byte>();
            var previousBlockHashBytes = new byte[32];
            var bitsBytes = new byte[8];
            var nonceBytes = new byte[8];
            var timestampBytes = new byte[8];

            previousBlockHashBytes = Base16.Decode(PreviousBlockHash);
            bitsBytes = BitConverter.GetBytes(Bits);
            nonceBytes = BitConverter.GetBytes(Nonce);
            timestampBytes = BitConverter.GetBytes(Timestamp);
            var generatorBytes = Base58.Decode(GeneratorId);
            var payloadBytes = Base16.Decode(PayloadHash);
            var blockSigBytes = Encoding.UTF8.GetBytes(BlockSignature);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bitsBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(timestampBytes);
            }

            bytes.AddRange(previousBlockHashBytes);
            bytes.AddRange(bitsBytes);
            bytes.AddRange(nonceBytes);
            bytes.AddRange(timestampBytes);
            bytes.AddRange(generatorBytes);
            bytes.AddRange(payloadBytes);
            bytes.AddRange(blockSigBytes);

            return Base16.Encode(
                HashHelper.Hash(
                    bytes.ToArray()
            ));
        }


        public override void Deserialize(byte[] bytes, ref int index)
        {
            var versionBytes = new byte[4];
            var hashBytes = new byte[32];
            var heightBytes = new byte[8];
            var previousBlockHashBytes = new byte[32];
            var bitsBytes = new byte[8];
            var nonceBytes = new byte[8];
            var timestampBytes = new byte[8];
            var generatorIdBytes = new byte[28];
            var payloadHashBytes = new byte[32];
            var blockSigSizeBytes = new byte[4];
            var totalTransactionBytes = new byte[4];

            Array.Copy(bytes, index, versionBytes, 0, versionBytes.Length);
            index += versionBytes.Length;

            Array.Copy(bytes, index, hashBytes, 0, hashBytes.Length);
            index += hashBytes.Length;

            Array.Copy(bytes, index, heightBytes, 0, heightBytes.Length);
            index += heightBytes.Length;

            Array.Copy(bytes, index, previousBlockHashBytes, 0, previousBlockHashBytes.Length);
            index += previousBlockHashBytes.Length;

            Array.Copy(bytes, index, bitsBytes, 0, bitsBytes.Length);
            index += bitsBytes.Length;

            Array.Copy(bytes, index, nonceBytes, 0, nonceBytes.Length);
            index += nonceBytes.Length;

            Array.Copy(bytes, index, timestampBytes, 0, timestampBytes.Length);
            index += timestampBytes.Length;

            Array.Copy(bytes, index, generatorIdBytes, 0, generatorIdBytes.Length);
            index += generatorIdBytes.Length;

            Array.Copy(bytes, index, payloadHashBytes, 0, payloadHashBytes.Length);
            index += payloadHashBytes.Length;

            Array.Copy(bytes, index, blockSigSizeBytes, 0, blockSigSizeBytes.Length);
            index += blockSigSizeBytes.Length;


            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(versionBytes);
                Array.Reverse(heightBytes);
                Array.Reverse(bitsBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(timestampBytes);
                Array.Reverse(blockSigSizeBytes);
            }

            this.Version = BitConverter.ToInt32(versionBytes, 0);
            this.Hash = Base16.Encode(hashBytes);
            this.Height = BitConverter.ToInt64(heightBytes, 0);
            this.PreviousBlockHash = Base16.Encode(previousBlockHashBytes);
            this.Bits = BitConverter.ToInt64(bitsBytes, 0);
            this.Nonce = BitConverter.ToInt64(nonceBytes, 0);
            this.Timestamp = BitConverter.ToInt64(timestampBytes, 0);
            this.GeneratorId = Base58.Encode(generatorIdBytes);
            //this.GenerationSignature = Base16.Encode(generationSignatureBytes);
            //this.CumulativeDifficulty = Encoding.UTF8.GetString(cumulativeDiffTextBytes.ToArray());
            this.PayloadHash = Base16.Encode(payloadHashBytes);
            this.BlockSigSize = BitConverter.ToInt32(blockSigSizeBytes, 0);

            var blockSigBytes = new byte[this.BlockSigSize];
            Array.Copy(bytes, index, blockSigBytes, 0, blockSigBytes.Length);
            index += blockSigBytes.Length;

            Array.Copy(bytes, index, totalTransactionBytes, 0, totalTransactionBytes.Length);
            index += totalTransactionBytes.Length;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(totalTransactionBytes);
            }

            this.BlockSignature = Encoding.UTF8.GetString(blockSigBytes);
            this.TotalTransaction = BitConverter.ToInt32(totalTransactionBytes, 0);
        }

        public override byte[] Serialize()
        {
            var bytes = new List<byte>();
            var versionBytes = new byte[4];
            var hashBytes = new byte[32];
            var heightBytes = new byte[8];
            var previousBlockHashBytes = new byte[32];
            var bitsBytes = new byte[8];
            var nonceBytes = new byte[8];
            var timestampBytes = new byte[8];
            var payloadHashBytes = new byte[32];
            var blockSigSizeBytes = new byte[4];
            var totalTransactionBytes = new byte[4];

            versionBytes = BitConverter.GetBytes(Version);
            hashBytes = Base16.Decode(Hash);
            heightBytes = BitConverter.GetBytes(Height);
            previousBlockHashBytes = Base16.Decode(PreviousBlockHash);
            bitsBytes = BitConverter.GetBytes(Bits);
            nonceBytes = BitConverter.GetBytes(Nonce);
            timestampBytes = BitConverter.GetBytes(Timestamp);
            var generatorIdBytes = Base58.Decode(GeneratorId);
            payloadHashBytes = Base16.Decode(PayloadHash);
            blockSigSizeBytes = BitConverter.GetBytes(BlockSigSize);
            var blockSigBytes = Encoding.UTF8.GetBytes(BlockSignature);

            totalTransactionBytes = BitConverter.GetBytes(TotalTransaction);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(versionBytes);
                Array.Reverse(heightBytes);
                Array.Reverse(bitsBytes);
                Array.Reverse(nonceBytes);
                Array.Reverse(timestampBytes);
                Array.Reverse(blockSigSizeBytes);
                Array.Reverse(totalTransactionBytes);
            }

            bytes.AddRange(versionBytes);
            bytes.AddRange(hashBytes);
            bytes.AddRange(heightBytes);
            bytes.AddRange(previousBlockHashBytes);
            bytes.AddRange(bitsBytes);
            bytes.AddRange(nonceBytes);
            bytes.AddRange(timestampBytes);
            bytes.AddRange(generatorIdBytes);
            bytes.AddRange(payloadHashBytes);
            bytes.AddRange(blockSigSizeBytes);
            bytes.AddRange(blockSigBytes);

            bytes.AddRange(totalTransactionBytes);

            return bytes.ToArray();
        }

        public override string ToString()
        {
            var formatString = "Version = {0} " +
                               "Hash = {1} " +
                               "Height = {2} " +
                               "PreviousBlockHash = {3} " +
                               "Bits = {4} " +
                               "Nonce = {5} " +
                               "Timestamp = {6} " +
                               "GeneratorId = {7} " +
                               "PayloadHash = {8} " +
                               "BlockSigSize = {9} " +
                               "BlockSignature = {10} " +
                               "TotalTransaction = {11}"
                               ;
            return string.Format(formatString,
                Version,
                Hash,
                Height,
                PreviousBlockHash,
                Bits,
                Nonce,
                Timestamp,
                GeneratorId,
                PayloadHash,
                BlockSigSize,
                BlockSignature,
                TotalTransaction
                );
        }

        public string GetKey()
        {
            return $"{Height}_{Hash}";
        }
    }
}