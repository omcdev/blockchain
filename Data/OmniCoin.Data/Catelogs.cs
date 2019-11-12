


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Data
{
    public class UserSetting
    {
        public const string AddressBook = "AddressBook";
        public const string AccountBook = "AccountBook";
        public const string TxCommentBook = "TxCommentBook";
        public const string PaymentRequestBook = "PaymentRequestBook";
    }

    public class UserTables
    {
        public const string Account = "Account";
        public const string AddressItem = "AddressItem";
        public const string TxComment = "TxComment";
        public const string PaymentRequest = "PaymentRequest";
    }

    public class BlockDataSetting
    {
        public const string LatestBlockHeight = "LatestBlockHeight";
        public const string MiningPools = "MiningPools";
    }

    public class BlockTables
    {
        public const string Block = "Block";
        public const string Link_Block_Height_Hash = "Link_Block_Height_Hash";
        public const string Link_Account_Utxo = "Link_Account_Utxo";
        /// <summary>
        /// 有维护，但暂无使用
        /// </summary>
        public const string Link_Height_UpdateUtxo = "Link_Height_UpdateUtxo";
        public const string UtxoSet = "UtxoSet";
        public const string MiningPool = "MiningPool";
    }

    public class AppSetting
    {
        public const string DefaultAccount = "DefaultAccount";

        public const string PaymentAccountBook = "PaymentAccountBook";

        public const string FeePerKB = "FeePerKB";
        public const string Encrypt = "Encrypt";
        public const string PassCiphertext = "PassCiphertext";
        
        public const string PaymentBook = "PaymentBook";
        public const string BlackPeers = "BlackPeers";

        public const string Version = "Version";
    }

    public class AppTables
    {
        public const string TxPoolItem = "TxPoolItem";
        /// <summary>
        /// 未维护
        /// </summary>
        public const string Utxo_TxLinkItem = "Utxo_TxLinkItem";
        public const string TradeRecord = "TradeRecord";
    }

    public class ExplorerSetting
    {
        public const string DataStatistics = "DataStatistics";
    }
}