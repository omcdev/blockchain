


using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OmniCoin.Consensus
{
    public class Script
    {
        const string UNLOCK_SCRIPT_TEMPLATE = "{0}[ALL] {1}";
        const string UNLOCK_SCRIPT_REGEX = @"(\w+)\[ALL\] (\w+)";
        const string LOCK_SCRIPT_TEMPLATE = "OP_DUP OP_HASH160 {0} OP_EQUALVERIFY OP_CHECKSIG";
        const string LOCK_SCRIPT_REGEX = @"OP_DUP OP_HASH160 (\w+) OP_EQUALVERIFY OP_CHECKSIG";


        public static string BuildLockScipt(string receiverId)
        {
            return string.Format(
                LOCK_SCRIPT_TEMPLATE,
                Base16.Encode(
                    AccountIdHelper.GetPublicKeyHash(receiverId)
                ));
        }

        public static string BuildUnlockScript(string transactionHash, int outputIndex, byte[] privateKey, byte[] publicKey)
        {
            var data = new List<byte>();
            data.AddRange(Base16.Decode(transactionHash));

            var indexBytes = BitConverter.GetBytes(outputIndex);

            if(BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            data.AddRange(indexBytes);

            using (var dsa = ECDsa.ImportPrivateKey(privateKey))
            {
                if(Base16.Encode(dsa.PublicKey) != Base16.Encode(publicKey))
                {
                    throw new CommonException(ErrorCode.Engine.Transaction.Verify.PRIVATE_KEY_IS_ERROR);
                }

                var signature = dsa.SingnData(data.ToArray());

                return string.Format(
                    UNLOCK_SCRIPT_TEMPLATE,
                    Base16.Encode(signature),
                    Base16.Encode(dsa.PublicKey)
                    );
            }
        }

        public static string BuildMinerScript(string minerInfo,string remark)
        {
            return Base16.Encode(Encoding.UTF8.GetBytes(minerInfo + "`" + remark));
        }

        public static string GetPublicKeyHashFromLockScript(string lockScript)
        {
            var result = Regex.Matches(lockScript, LOCK_SCRIPT_REGEX);

            if(result.Count > 0)
            {
                return result[0].Groups[1].Value;
            }
            else
            {
                return null;
            }
        }

        public static bool GetParametersInUnlockScript(string unlockScript, out string sinagure, out string publicKey)
        {
            var result = Regex.Matches(unlockScript, UNLOCK_SCRIPT_REGEX);

            if(result.Count == 2)
            {
                sinagure = result[0].Value;
                publicKey = result[1].Value;

                return true;
            }
            else
            {
                sinagure = null;
                publicKey = null;
                return false;
            }
        }

        public static bool VerifyLockScriptFormat(string lockScript)
        {
            return Regex.IsMatch(lockScript, LOCK_SCRIPT_REGEX);
        }

        public static bool VerifyUnlockScriptFormat(string unlockScript)
        {
            return Regex.IsMatch(unlockScript, UNLOCK_SCRIPT_REGEX);
        }

        public static bool VerifyLockScriptByUnlockScript(string transactionHash, int outputIndex, string lockScript, string unlockScript)
        {
            var data = new List<byte>();
            data.AddRange(Base16.Decode(transactionHash));

            var indexBytes = BitConverter.GetBytes(outputIndex);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(indexBytes);
            }

            data.AddRange(indexBytes);

            var parameters = unlockScript.Split("[ALL] ");
            var dsa = ECDsa.ImportPublicKey(Base16.Decode(parameters[1]));
            var signatureResult = dsa.VerifyData(data.ToArray(), Base16.Decode(parameters[0]));

            if(signatureResult)
            {
                var publicKeyHash = GetPublicKeyHashFromLockScript(lockScript);
                return Base16.Encode(HashHelper.Hash160(Base16.Decode(parameters[1]))) == publicKeyHash;
            }
            else
            {
                return false;
            }
        }
    }
}
