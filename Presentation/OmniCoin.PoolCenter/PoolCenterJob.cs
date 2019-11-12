


using OmniCoin.Consensus;
using OmniCoin.Entities;
using OmniCoin.Framework;
using OmniCoin.Messages;
using OmniCoin.MiningPool.Shares;
using OmniCoin.Pool.Redis;
using OmniCoin.PoolCenter.Apis;
using OmniCoin.PoolCenter.Helper;
using OmniCoin.PoolMessages;
using OmniCoin.RabbitMQ;
using OmniCoin.ShareModels;
using OmniCoin.ShareModels.Msgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace OmniCoin.PoolCenter
{
    public class PoolCenterJob
    {
        long _startTime;
        byte[] _poolPrivateKey;
        PoolArgs _startArgs;
        int _generatedBlockCount = 0;

        public Dictionary<string, long> RemoveStartMsgIds = new Dictionary<string, long>();
        const long MaxTaskTime = 600000; //600000  10 min
        const long RedisKeepTime = 3600000; //1 hour
        public StartMiningMsg CurrentStartMiningMsg;
        Timer timer;
        long currentBlockStartTime = 0;
        long currentBlockHeight = 0;
        public static PoolCenterJob Current;

        public PoolCenterJob(PoolArgs args)
        {
            //判断钱包是否加密和密码是否为空
            string password = "";
            if (NodeApi.Current.GetTxSettings().Encrypt)
            {
                if (string.IsNullOrEmpty(args.Password))
                {
                    Console.Write("Please enter your wallet password:");
                    ConsoleKeyInfo info = Console.ReadKey(true);
                    while (info.Key != ConsoleKey.Enter)
                    {
                        Console.Write("*");
                        password += info.KeyChar;
                        info = Console.ReadKey(true);
                    }
                    Console.WriteLine();
                    if (string.IsNullOrEmpty(password))
                    {
                        Console.WriteLine("password can not be empty");
                        return;
                    }
                }
                else
                {
                    password = args.Password;
                }
            }


            _startArgs = args;
            try
            {
                //获取钱包私钥
                _poolPrivateKey = NodeApi.Current.GetPrivateKey(args.Account, password);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
        
        public void Start()
        {
            try
            {
                StartMining();
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.ToString());
                LogHelper.Info("Start Failure ,Wait Restart");
                Task.Delay(5000).Wait();
                Start();
            }
        }

        /// <summary>
        /// 定时监听任务
        /// </summary>
        public void StartListen()
        {
            Task.Run(() =>
            {
                timer = new Timer();
                timer.Interval = 30000;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
            });
        }

        /// <summary>
        /// 超过最大任务时间就重启挖矿
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                var starttime = _startTime;
                if (starttime != _startTime)
                {
                    starttime = _startTime;
                    return;
                }
                else if (Time.EpochTime - starttime > MaxTaskTime)
                {
                    StopMining(false, CurrentStartMiningMsg.Id);
                    LogHelper.Info("Send StopMsg");
                    Task.Delay(Setting.MaxMiningBlockCount).Wait();
                    StartMining();
                    LogHelper.Info("Send StartMsg");
                }
                //从redis和Dictionary中删除超过RedisKeepTime的数据
                var currentTime = Time.EpochTime;
                var ids = RemoveStartMsgIds.Where(x => currentTime - x.Value > RedisKeepTime).Select(x=>x.Key).ToList();
                ids.ForEach(x => RedisManager.Current.RemoveDataInRedis(x));
                ids.ForEach(x => RemoveStartMsgIds.Remove(x));
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex.ToString());
            }
            finally
            {
                timer.Start();
            }
        }

        #region 开始挖矿
        private void StartMining()
        {
            var block = NodeApi.Current.GenerateMiningBlock(_startArgs.Name, _startArgs.Account);
            var currentScoopNumber = POC.GetScoopNumber(block.Header.PayloadHash, block.Header.Height);
            _startTime = Time.EpochTime;
            block.Header.Timestamp = _startTime;

            if (block.Header.Height != this.currentBlockHeight || this.currentBlockStartTime <= 0)
            {
                this.currentBlockStartTime = _startTime;
                this.currentBlockHeight = block.Header.Height;
            }

            StartMiningMsg startMiningMsg = StartMiningMsg.CreateNew();
            startMiningMsg.BlockHeight = block.Header.Height;
            startMiningMsg.ScoopNumber = currentScoopNumber;
            startMiningMsg.StartTime = _startTime;
            var genHash = BlockHelper.GenHash(block.Header.PayloadHash, block.Header.Height);
            startMiningMsg.GenHash = Base16.Encode(genHash);
            startMiningMsg.BaseTarget = NodeApi.Current.GetBaseTarget(block.Header.Height);

            var blockKey = KeyHelper.GetBlockKey(startMiningMsg.Id);

            RedisManager.Current.SaveDataToRedis(blockKey, block);
            //MQApi.SendStartMsg(startMiningMsg);
            RabbitMQApi.SendStartMsg(startMiningMsg);

            CurrentStartMiningMsg = startMiningMsg;
            LogHelper.Info("Start MiningTask Id=" + startMiningMsg.Id + " ScoopNumber = " + startMiningMsg.ScoopNumber + " Height= " + startMiningMsg.BlockHeight);
            LogHelper.Info($"Block bits is {POC.ConvertBitsToBigInt(block.Header.Bits).ToString("X").PadLeft(64, '0')}");
            LogHelper.Info($"StartMiningMsg.BaseTarget is {POC.ConvertBitsToBigInt(startMiningMsg.BaseTarget).ToString("X").PadLeft(64, '0')}");
            LogHelper.Info($"Block height is {block.Header.Height}");
        }
        #endregion

        #region 停止挖矿
        public void StopMining(bool result, string startId)
        {
            var stopMsg = StopMiningMsg.CreateNew();
            stopMsg.StopTime = Time.EpochTime;
            var block = RedisManager.Current.GetDataInRedis<BlockMsg>(startId);
            stopMsg.CurrentHeight = Convert.ToInt32(block.Header.Height);
            stopMsg.StartMsgId = startId;
            if (result)
            {
                stopMsg.StopReason = StopReason.MiningSucesses;
                _generatedBlockCount++;
            }
            else
            {
                stopMsg.StopReason = StopReason.IsMininged;
            }
            //MQApi.SendStopMsg(stopMsg);
            RabbitMQApi.SendStopMsg(stopMsg);
            if (!RemoveStartMsgIds.ContainsKey(stopMsg.StartMsgId))
                RemoveStartMsgIds.Add(stopMsg.StartMsgId, stopMsg.StopTime);
        }
        #endregion

        #region 生成区块
        public bool ForgeBlock(string startId, string minerAddress, long nonce,out BlockMsg successBlock)
        {
            successBlock = null;
            var block = RedisManager.Current.GetDataInRedis<BlockMsg>(startId);
            if (block == null)
                return false;

            LogHelper.Info("Forge Block 7");

            //区块由矿池生成，奖励由矿池分配给矿工
            block.Header.GeneratorId = minerAddress;
            block.Header.Nonce = nonce;
            block.Header.Timestamp = Time.EpochTime;

            var dsa = ECDsa.ImportPrivateKey(_poolPrivateKey);

            block.Header.BlockSignature = Base16.Encode(dsa.SingnData(Base16.Decode(block.Header.PayloadHash)));
            block.Header.BlockSigSize = block.Header.BlockSignature.Length;
            block.Header.Hash = block.Header.GetHash();
            block.Header.TotalTransaction = block.Transactions.Count;

            var result = NodeApi.Current.ForgeBlock(block);

            if(result)
            {
                //关闭挖矿
                PoolCenterJob.Current.StopMining(true, startId);
                RedisManager.Current.SaveDataToRedis(startId, block);
                successBlock = block;
                var currentTime = Time.EpochTime;
                var startTime = block.Header.Timestamp;

                if(block.Header.Height == this.currentBlockHeight)
                {
                    startTime = this.currentBlockStartTime;
                    LogHelper.Info($"block start time is {Time.GetLocalDateTime(startTime)}");
                }

                //ConfigurationTool.GetAppSettings<PoolCenterSetting>("PoolCenterSetting").GenerateBlockDelayTime;
                int generateBlockDelaySeconds = ConfigCenter.ConfigPoolCenter.GenerateBlockDelayTime;
                if (currentTime - startTime < generateBlockDelaySeconds)
                {
                    var waitTime = generateBlockDelaySeconds - (int)(currentTime - startTime);
                    LogHelper.Info($"Block generate time {currentTime - startTime} less than {generateBlockDelaySeconds/1000} seconds, wait for a moment");
                    System.Threading.Thread.Sleep(waitTime);
                }
            }

            LogHelper.Info("ForgeBlock Result = "+ result);
            return result;
        }
        #endregion

        #region 保存区块生成速率
        private void SaveBlockRates()
        {
            PoolApi.SaveBlockRates();
        }
        #endregion

        #region 保存Hash生成速率
        private void SaveHashRates()
        {
            PoolApi.SaveHashRates();
        }
        #endregion
    }
}