using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Business
{
    public class AwardSetting
    {
        ///// <summary>
        ///// 手续费比例
        ///// </summary>
        //public double ServiceFeeProportion { get; set; }

        ///// <summary>
        ///// 提成比例
        ///// </summary>
        //public double ExtractProportion { get; set; }

        ///// <summary>
        ///// 提成收款地址
        ///// </summary>
        //public string ExtractReceiveAddress { get; set; }

        public string PoolType { get; set; }

        public string NodeRpcMainnet { get; set; }

        public string NodeRpcTestnet { get; set; }

        public string PosCheckServiceUrlTestNet { get; set; }

        public string PosCheckServiceUrlMainNet { get; set; }

        public string SuperNodeAddress { get; set; }
    }
}
