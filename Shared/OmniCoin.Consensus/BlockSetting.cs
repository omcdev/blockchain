

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Framework;

namespace OmniCoin.Consensus
{
    public class BlockSetting
    {
        //Max block size is 12 MB
        public const long MAX_BLOCK_SIZE = 5 * 1024 * 1024;

        //public const long VERIFIED_BLOCKS = 6;

        public const long LOCK_TIME_MAX = 3153600000000; //micro senconds, 100 year

        //public const long MAX_SIGNATURE = 10;

        //public const long COINBASE_MATURITY = 100;

        public const long INPUT_AMOUNT_MAX = (long)4.5E+17;

        public const long OUTPUT_AMOUNT_MAX = (long)4.5E+17;

        public const long TRANSACTION_MIN_SIZE = 100;

        public const long TRANSACTION_MIN_FEE = 100;

        public const string GenesisBlockReceiver_Main = "omnimHxTpx8GsfAf6ek8nikUuaGzv9Mwo9DrMg";

        public const string GenesisBlockReceiver_Testnet = "omnitTsXYtE1QWADfPrGPEYEpqeY1A2SPUxDpi";

        public const string GenesisBlockRemark_Main = "A new energy efficient cryptocurrency with the balance of decentralization, security and performance designed by OmniCoin - Financial Intelligence, Innovation & Integrity";

        public const string GenesisBlockRemark_Testnet = "Introducing OmniCoin testnet during iWorld Chengdu 2018";

        public static string GenesisBlockRemark
        {
            get
            {
                return GlobalParameters.IsTestnet ? GenesisBlockRemark_Testnet : GenesisBlockRemark_Main;
            }
        }

        public static string GenesisBlockReceiver
        {
            get
            {
                return GlobalParameters.IsTestnet ? GenesisBlockReceiver_Testnet : GenesisBlockReceiver_Main;
            }
        }
    }
}
