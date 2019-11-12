


using System;

namespace OmniCoin.Pool
{
    public static class Setting
    {
        public static string PoolAddress;

        private static int POOL_MAIN_PORT = 0;
        private static int POOL_TEST_PORT = 0;

        public static int PoolPort
        {
            get
            {
                if (Framework.GlobalParameters.IsTestnet)
                {
                    return POOL_TEST_PORT;
                }
                else
                {
                    return POOL_MAIN_PORT;
                }
            }
        }

        public static void Init(int poolPortMainet,int poolPortTestnet)
        {
            POOL_MAIN_PORT = poolPortMainet;
            POOL_TEST_PORT = poolPortTestnet;
        }
        
        public const int MaxNonceCount = 262144;

        public const int BufferSize = Int16.MaxValue;

        public static string PoolId = $"PoolId:{Guid.NewGuid().ToString()}";

        public const int HEART_TIME = 1000 * 10;

        public static int Max_TCP_Count = 800;
    }
}
