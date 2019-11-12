// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Entities;
using FiiiChain.Framework;
using FiiiChain.Messages;
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace FiiiChain.Consensus
{
    class POW
    {
        private long height;
        const long initCoins = (long)2E+11;
        const long genesisBlockCoins = (long)2.5E+17;
        //block generate reward will be decuct half for every 500000 blocks 
        const long blockRewardHalveStep = 500000L;
        //micro second, 5 minutes
        const long blockGenerateInterval = 300000;
        //adjust diffiuclty for every 4032 blockes, 2 weeks
        public const long DiffiucltyAdjustStep = 4032;
        //the value of target when difficulty = 1
        readonly BigInteger target_1 = BigInteger.Parse("00000000FFFFFF00000000000000000000000000000000000000000000000000", NumberStyles.AllowHexSpecifier);
        const long defaultBits = 0x1E03E7FF; //0.0001:1E270FFF; 0.001:0x1E03E7FF; 0.01:0x1D63FFFF; 0.1:0x1D09FFFF; 1:0x1d00FFFF

        public POW(long height)
        {
            this.height = height;
        }

        public long CalculateNextWorkTarget(long lastBlockHeight, long lastBlockBits, BlockMsg prev4032Block)
        {
            if (lastBlockHeight < 0)
            {
                return defaultBits;
            }
            //not need adjust
            if (lastBlockHeight + 1 % DiffiucltyAdjustStep != 0)
            {
                return lastBlockBits;
            }
            else
            {
                var lastTarget = this.ConvertBitsToBigInt(lastBlockBits);
                var oldDifficulty = Math.Exp(BigInteger.Log(target_1) - BigInteger.Log(lastTarget));

                var targetTimespan = DiffiucltyAdjustStep * blockGenerateInterval;
                var actualTimespan = Time.EpochTime - prev4032Block.Header.Timestamp;

                if (actualTimespan < targetTimespan / 4)
                {
                    actualTimespan = targetTimespan / 4;
                }

                if (actualTimespan > targetTimespan * 4)
                {
                    actualTimespan = targetTimespan * 4;
                }

                var newDifficulty = oldDifficulty * (actualTimespan / targetTimespan);
                //var newDifficulty = 0.1;
                var newTargetText = (target_1 * 10000 / new BigInteger(Math.Round(newDifficulty,4) * 10000)).ToString("X");
                
                if(newTargetText.Length % 2 != 0)
                {
                    newTargetText = "0" + newTargetText;
                }

                var firstByte = Convert.ToByte(newTargetText.Substring(0, 2), 16);

                if(firstByte > 0x7f)
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

        public long GetNewBlockReward()
        {
            if(this.height == 0)
            {
                return genesisBlockCoins;
            }

            var times = this.height / blockRewardHalveStep;
            return initCoins / (int)(Math.Pow(2, times));
        }

        public BigInteger ConvertBitsToBigInt(long bits)
        {
            string bitsText = bits.ToString("X").PadLeft(8, '0');

            var exponent = Convert.ToInt32(bitsText.Substring(0, 2), 16);
            var coefficient = Convert.ToInt32(bitsText.Substring(2, 6), 16);

            var result = new BigInteger(coefficient) * BigInteger.Pow(new BigInteger(2), (0x08 * (exponent - 3)));
            return result;
        }

        public bool Verify(long bits, string hash)
        {
            try
            {
                //fix the bug for BigInteger negative value
                if(Convert.ToInt32(hash.Substring(0, 2), 16) >= 0x80)
                {
                    hash = "00" + hash;
                }

                var target = this.ConvertBitsToBigInt(bits);
                //var target = BigInteger.Parse("00000FFFFF000000000000000000000000000000000000000000000000000000", NumberStyles.AllowHexSpecifier);
                var current = BigInteger.Parse(hash, NumberStyles.AllowHexSpecifier);

                return current <= target;
            }
            catch(Exception)
            {
                return false;
            }
        }

        public long ConvertBitsToDifficulty(long bits)
        {
            var target = this.ConvertBitsToBigInt(bits);
            var difficulty = Math.Exp(BigInteger.Log(target_1) - BigInteger.Log(target));
            return Convert.ToInt64(difficulty);
        }
    }
}
