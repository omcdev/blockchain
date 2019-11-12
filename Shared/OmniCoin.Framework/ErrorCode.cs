


using System;
using System.Collections.Generic;
using System.Text;

namespace OmniCoin.Framework
{
    public class ErrorCode
    {
        public const int UNKNOWN_ERROR = 0000000;
        public class Engine
        {
            public const int COMMON_ERROR = 1000000;
            public class UTXO
            {
                public const int COMMON_ERROR = 1010000;
                public const int UTXO_IS_SPENT= 1010001;
            }

            public class Transaction
            {
                public const int COMMON_ERROR = 1020000;

                public class Verify
                {
                    public const int COMMON_ERROR = 1020100;
                    public const int TRANSACTION_HASH_ERROR = 1020101;
                    public const int TRANSACTION_HAS_BEEN_EXISTED = 1020102;
                    public const int INPUT_AND_OUTPUT_CANNOT_BE_EMPTY = 1020103;
                    public const int INPUT_EXCEEDED_THE_LIMIT = 1020104;
                    public const int OUTPUT_EXCEEDED_THE_LIMIT = 1020105;
                    public const int HASH_CANNOT_BE_EMPTY = 1020106;
                    public const int LOCK_TIME_EXCEEDED_THE_LIMIT = 1020107;
                    public const int TRANSACTION_SIZE_BELOW_THE_LIMIT = 1020108;
                    public const int NUMBER_OF_SIGNATURES_EXCEEDED_THE_LIMIT = 1020109;
                    public const int SCRIPT_FORMAT_ERROR = 1020110;
                    public const int UTXO_HAS_BEEN_SPENT = 1020111;
                    public const int COINBASE_UTXO_LESS_THAN_100_CONFIRMS = 1020112;
                    public const int OUTPUT_LARGE_THAN_INPUT = 1020113;
                    public const int TRANSACTION_FEE_IS_TOO_FEW = 1020114;
                    public const int UTXO_UNLOCK_FAIL = 1020115;
                    public const int UTXO_NOT_EXISTED = 1020116;
                    public const int COINBASE_FORMAT_ERROR = 1020117;
                    public const int COINBASE_OUTPUT_AMOUNT_ERROR = 1020118;
                    public const int PRIVATE_KEY_IS_ERROR = 1020119;
                    public const int TRANSACTION_IS_LOCKED = 1020120;
                    public const int COINBASE_NEED_100_CONFIRMS = 1020121;
                    public const int CHANGE_ADDRESS_IS_INVALID = 1020122;
                    public const int UTXO_DUPLICATED_IN_ONE_BLOCK = 1020123;
                    public const int SIGN_ADDRESS_NOT_EXISTS = 1020124;
                    public const int NOT_FOUND_COINBASE = 1020115;
                    public const int UTXO_DUPLICATED_IN_ONE_TRANSACTION = 1020126;
                    public const int UTXO_NEED_6_CONFIRMS = 1020127;                    
                    public const int DEPOSIT_TIME_NOT_EXPIRED = 1020128;//The deposit has not expired,can not be used

                }
            }

            public class Block
            {
                public const int COMMON_ERROR = 1030000;
                public class Verify
                {
                    public const int COMMON_ERROR = 1030100;
                    public const int BLOCK_HAS_BEEN_EXISTED = 1030101;
                    public const int BLOCK_HASH_ERROR = 1030102;
                    public const int BLOCK_SIZE_LARGE_THAN_LIMIT = 1030103;
                    public const int TRANSACTION_VERIFY_FAIL = 1030104;
                    public const int POC_VERIFY_FAIL = 1030105;
                    public const int BITS_IS_WRONG = 1030106;
                    public const int PREV_BLOCK_NOT_EXISTED = 1030107;
                    public const int BLOCK_TIME_IS_ERROR = 1030108;
                    public const int BLOCK_SIGNATURE_IS_ERROR = 1030109;
                    public const int GENERATION_SIGNATURE_IS_ERROR = 1030110;
                    public const int MINING_POOL_NOT_EXISTED = 1030111;
                    public const int MINING_POOL_EXISTED = 1030112;
                }
            }

            public class BlockChain
            {
                public const int COMMON_ERROR = 1040000;
                public const int ACCOUNT_ISNOT_MININGPOOL = 1040001;
            }

            public class Account
            {
                public const int COMMON_ERROR = 1050000;
            }

            public class P2P
            {
                public const int COMMON_ERROR = 1060000;

                public class Connection
                {
                    public const int COMMON_ERROR = 1060100;
                    public const int HOST_NAME_CAN_NOT_RESOLVED_TO_IP_ADDRESS = 1060101;
                    public const int THE_NUMBER_OF_CONNECTIONS_IS_FULL = 1060102;
                    public const int THE_PEER_IS_EXISTED = 1060103;
                    //public const int NOT_RECEIVED_PONG_MESSAGE = 1060104;
                    public const int NOT_RECEIVED_HEARTBEAT_MESSAGE_FOR_A_LONG_TIME = 1060105;
                    public const int P2P_VERSION_NOT_BE_SUPPORT_BY_REMOTE_PEER = 1060106;
                    public const int TIME_NOT_MATCH_WITH_RMOTE_PEER = 1060107;
                    public const int PEER_IN_BLACK_LIST = 1060108;
                }
            }

            public class Wallet
            {
                public const int COMMON_ERROR = 1070000;

                public const int DECRYPT_DATA_ERROR = 1070001;
                public const int DATA_SAVE_TO_FILE_ERROR = 1070002;
                public const int CHECK_PASSWORD_ERROR = 1070003;

                public class IO
                {
                    public const int COMMON_ERROR = 1070100;
                    public const int FILE_NOT_FOUND = 1070101;
                    public const int EXTENSION_NAME_NOT_SUPPORT = 1070102;
                    public const int FILE_DATA_INVALID = 1070103;
                }

                public class DB
                {
                    public const int COMMON_ERROR = 1080200;
                    public const int EXECUTE_SQL_ERROR = 1080201;
                    public const int LOAD_DATA_ERROR = 1080202;
                }
            }
        }

        public class Service
        {
            public const int COMMON_ERROR = 2000000;
            public class UTXO
            {
                public const int COMMON_ERROR = 2010000;
            }

            public class Transaction
            {
                public const int COMMON_ERROR = 2020000;
                public const int TO_ADDRESS_INVALID = 2020001;
                public const int BALANCE_NOT_ENOUGH = 2020002;
                public const int SEND_AMOUNT_LESS_THAN_FEE = 2020003;
                public const int FEE_DEDUCT_ADDRESS_INVALID = 2020004;
                public const int WALLET_DECRYPT_FAIL = 2020005;
                public const int DEPOSIT_TIME_MUST_LAGER_THEN_NOW = 2020006;
            }

            public class Account
            {
                public const int COMMON_ERROR = 2030000;

                public const int ACCOUNT_NOT_FOUND = 2030001;
                public const int DEFAULT_ACCOUNT_NOT_SET = 2030002;
            }

            public class AddressBook
            {
                public const int COMMON_ERROR = 2040000;
                public const int CAN_NOT_ADDED_SELF_ACCOUNT_INTO_ADDRESS_BOOK = 2040001;
            }

            public class Network
            {
                public const int COMMON_ERROR = 2050000;
                public const int P2P_SERVICE_NOT_START = 2050001;
                public const int NODE_EXISTED = 2050002;
                public const int NODE_NOT_EXISTED = 2050003;
                public const int NODE_IN_THE_BLACK_LIST = 2050004;
                public const int NODE_ADDRESS_FORMAT_INVALID = 2050005;
                public const int SET_BAN_COMMAND_PARAMETER_NOT_SUPPORTED = 2050006;
            }

            public class BlockChain
            {
                public const int COMMON_ERROR = 2060000;
                public const int SAME_HEIGHT_BLOCK_HAS_BEEN_GENERATED = 2060001;
                public const int BLOCK_DESERIALIZE_FAILED = 2060002;
                public const int BLOCK_SAVE_FAILED = 2060003;
            }

            public class Wallet
            {
                public const int COMMON_ERROR = 2070000;
                public const int CAN_NOT_ENCRYPT_AN_ENCRYPTED_WALLET = 2010001;
                public const int CAN_NOT_LOCK_AN_UNENCRYPTED_WALLET = 2010002;
                public const int CAN_NOT_UNLOCK_AN_UNENCRYPTED_WALLET = 2010003;
                public const int CAN_NOT_CHANGE_PASSWORD_IN_AN_UNENCRYPTED_WALLET = 2010004;
                public const int WALLET_HAS_BEEN_LOCKED = 2010005;
            }
        }
    }
}
