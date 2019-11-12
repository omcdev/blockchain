


using EdjCase.JsonRpc.Client;
using OmniCoin.Consensus;
using OmniCoin.DTO;
using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.PoolCenter.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OmniCoin.PoolCenter.Apis
{
    public class NodeApi : RpcBase
    {
        #region 初始化
        public NodeApi(string uri) : base(uri)
        {
        }

        public static NodeApi Current { get; set; }
        #endregion

        #region 生成待挖区块
        public BlockMsg GenerateMiningBlock(string poolName, string account)
        {
            var data = this.SendRpcRequest<string>("GenerateNewBlock", new object[] { poolName, account, 0 });
            var blockMsg = new BlockMsg();
            int index = 0;
            blockMsg.Deserialize(Base16.Decode(data), ref index);

            if(blockMsg.Transactions[0].Outputs[0].Amount < 0)
            {
                LogHelper.Warn("Coinbase output can not be less than 0");
                LogHelper.Warn("Block Info:" + data);

                throw new Exception("Coinbase output can not be less than 0");
            }
            
            //LogHelper.Warn("GenerateNewBlock Result :" + Newtonsoft.Json.JsonConvert.SerializeObject(blockMsg));
            return blockMsg;
        }
        #endregion

        #region 生成区块

        public bool ForgeBlock(BlockMsg blockMsg)
        {
            LogHelper.Info("Forge Block 8");
            var hashResult = BlockHelper.GetMiningWorkResult(blockMsg);

            LogHelper.Info("Height:" + blockMsg.Header.Height);
            LogHelper.Info("Bits:" + POC.ConvertBitsToBigInt(blockMsg.Header.Bits).ToString("X").PadLeft(64, '0'));
            LogHelper.Info("Hash:" + Base16.Encode(hashResult));
            LogHelper.Debug("Nonce:" + blockMsg.Header.Nonce);
            LogHelper.Debug("Address:" + blockMsg.Header.GeneratorId);
            if (!POC.Verify(blockMsg.Header.Bits, hashResult))
            {
                LogHelper.Info("POC Verify Failed");
                return false;
            }

            LogHelper.Info("Forge Block 9");
            var result = false;

            try
            {
                if (this.SubmitBlock(blockMsg))
                {
                    result = true;
                }
                LogHelper.Info($"Forge Block 10, {result}");
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
            finally
            {
                if(!result)
                {
                    Thread.Sleep(60 * 1000);
                    var nodeHeight = GetBlockHeight();

                    LogHelper.Info($"DB nodeHeight:{nodeHeight}, currentHeight:{blockMsg.Header.Height}");
                    if (nodeHeight >= blockMsg.Header.Height)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
            }

            return result;
        }
        #endregion

        #region 获取私钥
        public byte[] GetPrivateKey(string walletAddress, string password)
        {
            var setting = this.SendRpcRequest<GetTxSettingsOM>("GetTxSettings" , new object[] { });

            if (setting.Encrypt)
            {
                if (string.IsNullOrWhiteSpace(password) ||
                    !this.SendRpcRequest<bool>("WalletPassphrase", new object[] { password }))
                {
                    throw new Exception("Wallet unlock failed");
                }
            }

            var privateKeyText = this.SendRpcRequest<string>("DumpPrivKey", new object[] { walletAddress });

            if (string.IsNullOrWhiteSpace(privateKeyText))
            {
                throw new Exception("Initial error, please check wallet address and password");
            }

            if (setting.Encrypt)
            {
                this.SendRpcRequest("WalletLock");
            }

            return Base16.Decode(privateKeyText);
        }
        #endregion

        #region 获取区块高度
        public long GetBlockHeight()
        {
            return this.SendRpcRequest<long>("GetBlockCount");
        }
        #endregion

        #region 获取 BaseTarget
        public long GetBaseTarget(long height)
        {
            var target= this.SendRpcRequest<string>("GetBaseTarget", new object[] { height });
            return long.Parse(target);
        }
        #endregion

        #region 提交Block
        private bool SubmitBlock(BlockMsg block)
        {
            try
            {
                var blockData = Base16.Encode(block.Serialize());
                //TODO 测试 数据
                //LogHelper.Warn("SubmitBlock Src  >>  BlockMsg : " + Newtonsoft.Json.JsonConvert.SerializeObject(block) + "  ; Base16AfterSerialize : "+blockData);
                this.SendRpcRequest("SubmitBlock", new object[] { blockData });
                return true;
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.ToString());
                throw ex;
            }
        }
        #endregion

        public BlockMsg GetBlockByHeight(long height)
        {
            var hash = this.SendRpcRequest<string>("GetBlockHash", new object[] { height });

            if (string.IsNullOrWhiteSpace(hash))
            {
                return null;
            }

            var data = this.SendRpcRequest<string>("GetBlock", new object[] { hash, 0 });

            var blockMsg = new BlockMsg();
            int index = 0;
            blockMsg.Deserialize(Base16.Decode(data), ref index);

            return blockMsg;
        }

        public GetTxSettingsOM GetTxSettings()
        {
            var setting = this.SendRpcRequest<GetTxSettingsOM>("GetTxSettings");
            return setting;
        }
    }
}