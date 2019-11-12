using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.DTO.Explorer
{
    public class TransactionDetailOM
    {
        //ID
        public long Id { get; set; }

        //哈希
        public string Hash { get; set; }


        public string BlockHash { get; set; }

        public long BlockHeight { get; set; }

        public int Version { get; set; }

        public Boolean IsDiscarded { get; set; }

        //大小
        public int Size { get; set; }

        //收到时间
        public long Timestamp { get; set; }

        //锁定时间,0表示不锁定，其他数字就直接转成具体的日期时间
        public long LockTime { get; set; }

        //确认 （最新区块高度-当前区块高度）
        public decimal OutputAffirm { get; set; }

        //输入总额
        public decimal TotalInput { get; set; }

        //输出总额
        public decimal TotalOutput { get; set; }

        //交易费
        public decimal Fee { get; set; }

        public decimal OutputAmount { get; set; }

        public List<InputOM> InputList { get; set; }

        public List<OutputOM> OutputList { get; set; }

        public List<string> InputScriptList { get; set; }

        public List<string> OutputScriptList { get; set; }
    }
}
