

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Entities;
using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Linq;
using System.Globalization;
using OmniCoin.Messages;

namespace OmniCoin.Consensus
{
    public class POC
    {
        public const int MAX_SCOOP_NUMBER = 4095;
        public const int MAX_HASH_NUMBER = 8191;
        public const int MAX_SEED_LENGTH = 4096;
        public const long DIFFICULTY_CALCULATE_LOGIC_ADJUST_HEIGHT = 34560;
        const long INIT_COINS = (long)2E+10;
        const long GENESIS_BLOCK_COINS = (long)1E+15;
        //const long BLOCK_REWARD_HALVE_STEP = 100000L;//5000000L;
        const long BLOCK_REWARD_HALVE_STEP_Fix = 250000L;
        // 线上 测试环境 302A300506032B657003210005BBDC1CDACDA89F154C95A9155F2DE7AD6A53789C703EBAF6C332D3CFA1529A
        // 本地 测试环境 302A300506032B65700321000ABEA52E23CC24229B83A08DC8789582363EF628B3B9C0DB2F0147D877BD4BA2
        const string SUPER_NODE_PUBLIC_KEY_TEST = "302A300506032B657003210005BBDC1CDACDA89F154C95A9155F2DE7AD6A53789C703EBAF6C332D3CFA1529A";
        const string SUPER_NODE_PUBLIC_KEY_MAIN = "302A300506032B6570032100A0AF16B718EAB2B1ECAF26659C88734541AA74548CD69F62CA608500CADDDC6F";
        static string SUPER_NODE_PUBLIC_KEY
        {
            get
            {
                return GlobalParameters.IsTestnet ? SUPER_NODE_PUBLIC_KEY_TEST : SUPER_NODE_PUBLIC_KEY_MAIN;
            }
        }



        const long defaultBits = 0x1F0F423F; //0.00000001:0x2005F5E0; 0.0000001:0x20009896; 0.000001:0x1F0F423F; //0.00001:1F01869F; 0.0001:1E270FFF; 0.001:0x1E03E7FF; 0.01:0x1D63FFFF; 0.1:0x1D09FFFF; 1:0x1d00FFFF
        const long testnetDefaultBits = 0x20009896; //0.00000001:0x2005F5E0; 0.0000001:0x20009896; 0.000001:0x1F0F423F; //0.00001:1F01869F; 0.0001:1E270FFF; 0.001:0x1E03E7FF; 0.01:0x1D63FFFF; 0.1:0x1D09FFFF; 1:0x1d00FFFF
        public const long DIFFIUCLTY_ADJUST_STEP = 2880;//60;
        static readonly BigInteger target_1 = BigInteger.Parse("00000000FFFFFF00000000000000000000000000000000000000000000000000", NumberStyles.AllowHexSpecifier);
        static readonly BigInteger minimumTarget = BigInteger.Parse("7EFFFFFFFFFFFF00000000000000000000000000000000000000000000000000", NumberStyles.AllowHexSpecifier);
        const long blockGenerateInterval = 60 * 1000;
        
        public static long GetNewBlockReward(long height)
        {
            //if (height == 0)
            //{
            //    return GENESIS_BLOCK_COINS;
            //}
            //long times;
            //if(height < 100000)
            //{
            //    return INIT_COINS;
            //}else if (height >= 100000 && height < 350000)
            //{
            //    return INIT_COINS / 2;
            //}
            //times = height / BLOCK_REWARD_HALVE_STEP_Fix;
            //return INIT_COINS / (int)(Math.Pow(2, times));

            if (height == 0)
            {
                return GENESIS_BLOCK_COINS;
            }
            else if (height < 100000)
            {
                return INIT_COINS;
            }
            else if (height < 350000)
            {
                return INIT_COINS / 2;
            }
            else
            {
                return INIT_COINS / 2 / (int)(Math.Pow(2, (1 + (height - 350000) / BLOCK_REWARD_HALVE_STEP_Fix)));
            }

        }

        public static NonceData GenerateNonceData(string walletAddress, long nonce)
        {
            var nonceData = new NonceData();
            nonceData.Nonce = nonce;
            var hashList = new List<ScoopDataItem>();

            var addressBytes = Base58.Decode(walletAddress);
            var nonceBytes = BitConverter.GetBytes(nonce);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(nonceBytes);
            }

            var seed = new List<byte>();
            seed.AddRange(addressBytes);
            seed.AddRange(nonceBytes);
            var index = MAX_HASH_NUMBER;
            byte[] lastHash = null;

            while(index >= 0)
            {
                if(lastHash != null)
                {
                    seed.InsertRange(0, lastHash);
                }

                byte[] buffer;

                if(seed.Count > MAX_SEED_LENGTH)
                {
                    buffer = new byte[MAX_SEED_LENGTH];
                    Array.Copy(seed.ToArray(), buffer, buffer.Length);
                }
                else
                {
                    buffer = seed.ToArray();
                }

                var hash = Sha3Helper.Hash(buffer);
                hashList.Insert(0, new ScoopDataItem() { Index = index, Hash = hash });

                lastHash = hash;
                index--;
            }

            for(var i = 0; i <= MAX_SCOOP_NUMBER; i ++)
            {
                var scoopData = new ScoopData();
                scoopData.Index = i;
                scoopData.FirstData = hashList[i * 2];
                scoopData.SecondData = hashList[MAX_HASH_NUMBER - (i * 2)];

                nonceData.DataList.Add(scoopData);
            }

            //for(var i = 0; i<= MAX_SCOOP_NUMBER; i ++)
            //{
            //    nonceData.DataList[i].SecondData = hashList[MAX_HASH_NUMBER - (i * 2)];
            //}

            return nonceData;
        }

        public static byte[] CalculateScoopData(string walletAddress, long nonce, int scoopNumber)
        {
            var firstHashIndex = scoopNumber * 2;
            var secondHashIndex = MAX_HASH_NUMBER - (scoopNumber * 2);
            byte[] firstHash = null;
            byte[] secondHash = null;

            var addressBytes = Base58.Decode(walletAddress);
            var nonceBytes = BitConverter.GetBytes(nonce);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(nonceBytes);
            }

            var seed = new List<byte>();
            seed.AddRange(addressBytes);
            seed.AddRange(nonceBytes);
            var index = MAX_HASH_NUMBER;
            byte[] lastHash = null;

            while (index >= Math.Min(firstHashIndex, secondHashIndex))
            {
                if (lastHash != null)
                {
                    seed.InsertRange(0, lastHash);
                }

                byte[] buffer;

                if (seed.Count > MAX_SEED_LENGTH)
                {
                    buffer = new byte[MAX_SEED_LENGTH];
                    Array.Copy(seed.ToArray(), buffer, buffer.Length);
                }
                else
                {
                    buffer = seed.ToArray();
                }

                var hash = Sha3Helper.Hash(buffer);
                lastHash = hash;

                if(index == secondHashIndex)
                {
                    secondHash = hash;
                }
                else if(index == firstHashIndex)
                {
                    firstHash = hash;
                }
                
                index--;
            }

            var data = new List<byte>();
            data.AddRange(firstHash);
            data.AddRange(secondHash);

            return data.ToArray();
        }

        public static byte[] CalculateTargetResult(BlockMsg currentBlock)
        {
            var listBytes = new List<Byte>();
            listBytes.AddRange(Base16.Decode(currentBlock.Header.PayloadHash));
            listBytes.AddRange(BitConverter.GetBytes(currentBlock.Header.Height));
            var genHash = Sha3Helper.Hash(listBytes.ToArray());
            var scoopNumber = POC.GetScoopNumber(currentBlock.Header.PayloadHash, currentBlock.Header.Height);
            var scoopData = POC.CalculateScoopData(currentBlock.Header.GeneratorId, currentBlock.Header.Nonce, scoopNumber);
            List<byte> targetByteLists = new List<byte>();
            targetByteLists.AddRange(scoopData);
            targetByteLists.AddRange(genHash);
            var baseTarget = Sha3Helper.Hash(targetByteLists.ToArray());
            return baseTarget;
        }

        public static byte[] CalculateGenerationSignature(string prevGenSig, string prevGenratorAddress)
        {
            if(string.IsNullOrWhiteSpace(prevGenSig) || string.IsNullOrWhiteSpace(prevGenratorAddress))
            {
                return HashHelper.EmptyHash();
            }

            var genSigSeed = new List<byte>();
            genSigSeed.AddRange(Base16.Decode(prevGenSig));
            genSigSeed.AddRange(Base58.Decode(prevGenratorAddress));
            return Sha3Helper.Hash(genSigSeed.ToArray());
        }

        public static int GetScoopNumber(string payloadHash, long blockHeight)
        {
            var hashByte = Base16.Decode(payloadHash);
            var hashSeed = new List<byte>();
            hashSeed.AddRange(hashByte);
            var heightBytes = BitConverter.GetBytes(blockHeight);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(heightBytes);
            }

            hashSeed.AddRange(heightBytes);
            var genHash = Sha3Helper.Hash(hashSeed.ToArray());
            
            var bigScoopNumber = ConvertByesToBigInteger(genHash) % new BigInteger(MAX_SCOOP_NUMBER + 1);
            return (int)bigScoopNumber;
        }
        
        public static BigInteger ConvertBitsToBigInt(long bits)
        {
            string bitsText = bits.ToString("X").PadLeft(8, '0');

            var exponent = Convert.ToInt32(bitsText.Substring(0, 2), 16);
            var coefficient = Convert.ToInt32(bitsText.Substring(2, 6), 16);

            var result = new BigInteger(coefficient) * BigInteger.Pow(new BigInteger(2), (0x08 * (exponent - 3)));
            return result;
        }

        public static long CalculateBaseTarget(long currentHeight, long preBits,long preTimestamp, BlockMsg prevStepBlockMsg)
        {
            if (currentHeight <= 1)
            {
                if (GlobalParameters.IsTestnet)
                {
                    return testnetDefaultBits;
                }
                else
                {
                    return defaultBits;
                }
            }
            //not need adjust
            if ((currentHeight - 1) % DIFFIUCLTY_ADJUST_STEP != 0)
            {
                return preBits;
            }
            else
            {
                var bits = prevStepBlockMsg.Header.Bits;
                var lastTarget = ConvertBitsToBigInt(bits);
                var oldDifficulty = (double)((target_1 * (long)1E15) / lastTarget) / 1E15;

                var targetTimespan = DIFFIUCLTY_ADJUST_STEP * blockGenerateInterval;
                var actualTimespan = preTimestamp - prevStepBlockMsg.Header.Timestamp;

                if (actualTimespan < targetTimespan / 4)
                {
                    actualTimespan = targetTimespan / 4;
                }

                if (actualTimespan > targetTimespan * 4)
                {
                    actualTimespan = targetTimespan * 4;
                }

                var newDifficulty = oldDifficulty * ((double)targetTimespan / (double)actualTimespan);
                //var newDifficulty = 0.1;

                var newTargetText = minimumTarget.ToString("X");

                if (Math.Round(newDifficulty, 15) * 1.0E15 > 0)
                {
                    var newTarget = target_1 * (long)1E15 / new BigInteger(Math.Round(newDifficulty, 15) * 1.0E15);
                    if (newTarget < minimumTarget)
                    {
                        newTargetText = newTarget.ToString("X");
                    }
                }

                //    var newTargetText = "0000FFFFFFFFFF00000000000000000000000000000000000000000000000000";

                //var oldDifficulty = Math.Exp(BigInteger.Log(target_1) - BigInteger.Log(BigInteger.Parse(newTargetText, NumberStyles.AllowHexSpecifier)));
                if (newTargetText.Length % 2 != 0)
                {
                    newTargetText = "0" + newTargetText;
                }

                var firstByte = Convert.ToByte(newTargetText.Substring(0, 2), 16);

                if (firstByte > 0x7f)
                {
                    newTargetText = "00" + newTargetText;
                }

                var length = newTargetText.Length / 2;

                var bitsText = new StringBuilder();
                bitsText.Append(length.ToString("X").PadLeft(2, '0'));

                newTargetText.PadRight(6, '0');
                bitsText.Append(newTargetText.Substring(0, 6));

                return Convert.ToInt64(bitsText.ToString(), 16);
            }
        }

        public static long CalculateBaseTarget(long currentHeight, BlockMsg prevBlockMsg, BlockMsg prevStepBlockMsg)
        {
            long bits = 0L;
            if (prevBlockMsg == null)
            {
                bits = CalculateBaseTarget(currentHeight,  0, 0, prevStepBlockMsg);
            }
            else
            {
                bits = CalculateBaseTarget(currentHeight, prevBlockMsg.Header.Bits, prevBlockMsg.Header.Timestamp, prevStepBlockMsg);
            }
            return bits;
        }
        public static double CalculateDifficulty(long bits)
        {
            var newTarget = ConvertBitsToBigInt(bits);
            var difficulty = (double)((target_1 * (long)1E15) / newTarget) / 1E15;
            return difficulty;
        }

        public static bool VerifyBlockSignature(string data, string signature, string publicKey)
        {
            var dsa = ECDsa.ImportPublicKey(Base16.Decode(publicKey));
            var current = Base16.Decode(data);
            var signatureBytes = Base16.Decode(signature);
            var result = dsa.VerifyData(current, signatureBytes);
            return result;
        }

        public static bool VerifyMiningPoolSignature(string data, string signature)
        {
            var dsa = ECDsa.ImportPublicKey(Base16.Decode(SUPER_NODE_PUBLIC_KEY));
            var current = Base16.Decode(data);
            var signatureBytes = Base16.Decode(signature);
            var result = dsa.VerifyData(current, signatureBytes);
            return result;
        }


        public static bool Verify(long bits, string hash)
        {
            try
            {
                //fix the bug for BigInteger negative value
                if (Convert.ToInt32(hash.Substring(0, 2), 16) >= 0x80)
                {
                    hash = "00" + hash;
                }

                var target = ConvertBitsToBigInt(bits);
                //var target = BigInteger.Parse("00000FFFFF000000000000000000000000000000000000000000000000000000", NumberStyles.AllowHexSpecifier);
                var current = BigInteger.Parse(hash, NumberStyles.AllowHexSpecifier);

                return current <= target;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool Verify(long bits, byte[] bytes)
        {
            return Verify(bits, Base16.Encode(bytes));
        }

        public static BigInteger ConvertByesToBigInteger(byte[] bytes)
        {
            if(bytes.Length > 0)
            {
                var text = Base16.Encode(bytes);
                if (text.Length % 2 != 0)
                {
                    text = "0" + text;
                }


                var firstByte = Convert.ToByte(text.Substring(0, 2), 16);

                if (firstByte > 0x7f)
                {
                    text = "00" + text;
                }

                var value = BigInteger.Parse(text, NumberStyles.AllowHexSpecifier);
                return value;
            }
            else
            {
                return BigInteger.Zero;
            }
        }
        
   }
}
