namespace OmniCoin.MiningPool.Award
{
    public class AwardConfig
    {
        /// <summary>
        /// 手续费比例
        /// </summary>
        //public double ServiceFeeProportion { get; set; }

        ///// <summary>
        ///// 提成比例
        ///// </summary>
        //public double ExtractProportion { get; set; }

        /// <summary>
        /// 提成收款地址
        /// </summary>
        //public string ExtractReceiveAddress { get; set; }

        /// <summary>
        /// 找零地址
        /// </summary>
        public string ChangeAddress { get; set; }

        /// <summary>
        /// 费率
        /// </summary>
        public long FeeRate { get; set; }

        ///// <summary>
        ///// 发送时间间隔
        ///// </summary>
        //public long SendInterval { get; set; }

        /// <summary>
        /// 一次发送人数
        /// </summary>
        public int SendCount { get; set; }

        /// <summary>
        /// 循环休眠时间
        /// </summary>
        public int CircleSleepTime { get; set; }

        /// <summary>
        /// 更新Reward表休眠时间
        /// </summary>
        public int UpdateRewardSleepTime { get; set; }

        public string NodeRpcMainnet { get; set; }

        public string NodeRpcTestnet { get; set; }

        public string RabbitMqConnectString { get; set; }

        public string MySqlTestnetConnectString { get; set; }

        public string MySqlMainnetConnectString { get; set; }
    }
}
