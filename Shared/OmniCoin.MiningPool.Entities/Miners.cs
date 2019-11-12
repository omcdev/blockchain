using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public class Miners
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 钱包地址
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 用户Id
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// 类型 0：POS，1：手机
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 设备序列号
        /// </summary>
        public string SN { get; set; }

        /// <summary>
        /// 状态 0：enable，1：disable 
        /// 当同一个SN，不同的Address出现的时候，先把旧的Address记录设置为Disable，然后执行插入操作
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 写入时间戳，当Status改变的时候更新
        /// </summary>
        public long Timstamp { get; set; }

        /// <summary>
        /// 最后登陆时间时间戳，用户每次登录时更新
        /// </summary>
        public long LastLoginTime { get; set; }

        /// <summary>
        /// 未发放奖励
        /// </summary>
        public long UnpaidReward { get; set; }

        /// <summary>
        /// 已发放奖励
        /// </summary>
        public long PaidReward { get; set; }
    }
}
