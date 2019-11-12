using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.MiningPool.Entities
{
    public static class MiningPoolErrorCode
    {
        public const int COMMON_ERROR = 1000000;
        public static class Miners
        {
            public const int COMMON_ERROR = 1010000;
            public const int ADDRESS_IS_INVALID = 1010001;
            public const int ADDRESS_HAS_BEEN_USED = 1010002;
            public const int ACCOUNT_AND_SN_IS_NOT_MATCH = 1010003;
            public const int ADDRESS_NOT_EXIST = 1010004;
            public const int ACCOUNT_IS_LOCKED = 1010005;
            public const int SN_CODE_ERROR = 1010006;
            public const int MINING_FORBIDDEN = 1010007;
            public const int ACCOUNT_NOT_EXIST = 1010008;
            public const int SCOOPNUMBER_NOT_MATCH = 1010009;
            public const int MAXNONCE_IS_INVALID = 1010010;
            public const int SCOOP_DATA_IS_INVALID = 1010011;
            public const int SN_IS_NOT_MATCH_BIND_ADDRESS_SN = 1010012;
            public const int GET_POOL_INFO_ERROR = 1010013;
        }

        public static class Blocks
        {
            public const int COMMON_ERROR = 1020000;
        }

        public static class RewardList
        {
            public const int COMMON_ERROR = 1030000;
        }
    }
}
