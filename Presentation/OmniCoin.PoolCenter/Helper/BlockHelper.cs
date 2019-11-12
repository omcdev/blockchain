


using OmniCoin.Consensus;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.PoolCenter.Apis;
using System;
using System.Collections.Generic;

namespace OmniCoin.PoolCenter.Helper
{
    public class BlockHelper
    {
        public static byte[] GenHash(string payloadHash, long blockHeight)
        {
            var payloadBytes = Base16.Decode(payloadHash);
            var heightBytes = BitConverter.GetBytes(blockHeight);
            var hashSeed = new List<byte>();
            hashSeed.AddRange(payloadBytes);
            hashSeed.AddRange(heightBytes);
            return Sha3Helper.Hash(hashSeed.ToArray());
        }

        public static byte[] GetMiningWorkResult(BlockMsg block)
        {
            var listBytes = new List<Byte>();
            listBytes.AddRange(Base16.Decode(block.Header.PayloadHash));
            listBytes.AddRange(BitConverter.GetBytes(block.Header.Height));
            var genHash = Sha3Helper.Hash(listBytes.ToArray());
            var scoopNumber = POC.GetScoopNumber(block.Header.PayloadHash, block.Header.Height);
            var scoopData = POC.CalculateScoopData(block.Header.GeneratorId, block.Header.Nonce, scoopNumber);
            List<byte> targetByteLists = new List<byte>();
            targetByteLists.AddRange(scoopData);
            targetByteLists.AddRange(genHash);
            var baseTarget = Sha3Helper.Hash(targetByteLists.ToArray());
            return baseTarget;
        }
    }
}
