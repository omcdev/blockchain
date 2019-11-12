


using OmniCoin.Data.Dacs;
using OmniCoin.Data.Entities;
using OmniCoin.Entities.CacheModel;
using OmniCoin.Framework;
using OmniCoin.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OmniCoin.Data
{
    public class DbDomains
    {
        public static LevelDomain UserDomain;
        public static LevelDomain BlockDomain;
        public static LevelDomain AppDomain;
        public static LevelDomain ExplorerDomain;

        const string StorageFileMainnet = "Storage";

        const string TempFileMainnet = "Temp";
        const string TempFileTestnet = "Temp_test";

        const string UserFile = "User";
        const string BlockFile = "Block";
        const string AppFile = "App";
        const string ExplorerFile = "Explorer";

        const string StorageFileTestnet = "Storage_Test";

        public const string TxContainer = "Transaction";

        public static string StorageFile
        {
            get
            {
                return GlobalParameters.IsTestnet ? StorageFileTestnet : StorageFileMainnet;
            }
        }

        public static string TempFile
        {
            get
            {
                return GlobalParameters.IsTestnet ? TempFileTestnet : TempFileMainnet;
            }
        }

        public static string EmptyHash;

        public static void Init()
        {
            EmptyHash = Base16.Encode(HashHelper.EmptyHash());
            var tempfile = TempFile;
            if (Directory.Exists(tempfile))
            {
                Directory.CreateDirectory(tempfile);
            }
            UserDomain = new LevelDomain(Path.Combine(tempfile, UserFile));
            BlockDomain = new LevelDomain(Path.Combine(tempfile, BlockFile));
            AppDomain = new LevelDomain(Path.Combine(tempfile, AppFile));
            ExplorerDomain = new LevelDomain(Path.Combine(tempfile, ExplorerFile));
        }
        /// <summary>
        /// 修复交易记录，导入地址之后需要对交易记录进行重新加载
        /// </summary>
        public static void InitData()
        {
            LogHelper.Debug("Start Init");
            var accounts = AccountDac.Default.GetAccountBook();
            var lastBlock = BlockDac.Default.SelectLast();
            if (lastBlock == null || lastBlock.Header.Height == 0)
            {
                AppDomain.Put(AppSetting.PaymentAccountBook, accounts);
                return;
            }

            LogHelper.Debug("Start Init UtxoSet");
            UtxoSetDac.Default.Init();
            Console.Write(Environment.NewLine);
            LogHelper.Debug("End Init UtxoSet");

            if (GlobalParameters.IsExplorer)
            {
                LogHelper.Debug("Load DataStatistics");
                LoadDataStatistics(lastBlock);
            }

            //加载交易记录
            if (!GlobalParameters.IsLoadTransRecord)
                return;

            LogHelper.Debug("Payments Initing");
            var myUtxos = UtxoSetDac.Default.GetByAccounts(accounts);

            var version = AppDac.Default.GetVersion();
            if (AppDac.AppVersion.Equals(version))
            {
                if (!myUtxos.Any())
                {
                    AppDomain.Put(AppSetting.PaymentAccountBook, accounts);
                    LogHelper.Debug("Transaction is Empty!!!");
                    return;
                }

                var paymentAccounts = AppDomain.Get<List<string>>(AppSetting.PaymentAccountBook) ?? new List<string>();
                if (accounts.Count == paymentAccounts.Count())
                    return;

                var localAccounts = accounts.ToList();
                localAccounts.RemoveAll(x => paymentAccounts.Contains(x));
                if (!localAccounts.Any())
                    return;

                var utxosetKeys = UtxoSetDac.Default.GetUtxoSetKeysByAccounts(localAccounts);
                //新加的地址，如果没有任何UtxoSet，不重新初始化交易记录
                if (!utxosetKeys.Any())
                {
                    AppDomain.Put(AppSetting.PaymentAccountBook, accounts);
                    return;
                }
            }
            PaymentDac.Default.Clear();

            var blockHeights = myUtxos.Select(x => x.BlockHeight).ToList();
            blockHeights.AddRange(myUtxos.Select(x => x.SpentHeight));
            blockHeights = blockHeights.Distinct().OrderByDescending(x => x).ToList();

            if (!blockHeights.Any())
            {
                LogHelper.Debug("blockHeights is Empty!!!");
                return;
            }

            //大部分的地址是没有交易记录的,减少地址的干扰可以加快初始化效率
            var usedAccounts = myUtxos.Select(x => x.Account).Distinct().ToList();
            LogHelper.Debug("Start Init TransactionRecord");
            LoadPayments(blockHeights, usedAccounts);
            //初始化了哪些交易记录
            AppDomain.Put(AppSetting.PaymentAccountBook, accounts);
            LogHelper.Debug("End Init TransactionRecord");
            AppDac.Default.UpdateVersion();

        }

        private static void LoadPayments(IEnumerable<long> heights, List<string> accounts)
        {
            var index = 1;
            var count = heights.Count();
            var hashes = BlockDac.Default.GetBlockHashByHeight(heights);

            List<PaymentCache> payments = new List<PaymentCache>();
            object enter1 = new object();
            object enter2 = new object();

            //var origRow = Console.CursorTop;
            //var origCol = Console.CursorLeft;
            Parallel.ForEach(hashes, hash =>
            {
                var block = BlockDac.Default.SelectByHash(hash);
                if (block == null)
                    return;
                var updateData = block.GetBlockUpdateData();

                var group = updateData.NewUtxoSet.GroupBy(x => x.TransactionHash);
                foreach (var transaction in block.Transactions)
                {
                    var newPayments = transaction.GetPayments(accounts);
                    if (newPayments.Any())
                    {
                        Monitor.Enter(enter2);
                        payments.AddRange(newPayments);
                        Monitor.Exit(enter2);
                    }
                }
                Monitor.Enter(enter1);
                //Console.SetCursorPosition(origCol, origRow);
                //Console.Write($"Load Payment {index}/{count}");
                if(index % 1000 == 0 || index == count)
                {
                    LogHelper.Info($"Load Payment {index}/{count}");
                }
                Monitor.Exit(enter1);
                index++;
            });

            if (payments.Any())
            {
                PaymentDac.Default.Insert(payments);
            }

            //foreach (var blockHeight in heights)
            //{
            //    payments.Clear();
            //    LogHelper.Debug($"Load Transactions {index}/{count}");
            //    index++;
            //    var block = BlockDac.Default.SelectByHeight(blockHeight);
            //    if (block == null)
            //        continue;
            //    var updateData = block.GetBlockUpdateData();

            //    var group = updateData.NewUtxoSet.GroupBy(x => x.TransactionHash);
            //    foreach (var transaction in block.Transactions)
            //    {
            //        var newPayments = transaction.GetPayments(accounts);
            //        if (newPayments.Any())
            //            payments.AddRange(newPayments);
            //    }

            //    if (payments.Any())
            //    {
            //        PaymentDac.Default.Insert(payments);
            //    }
            //}
        }

        private static void LoadDataStatistics(BlockMsg lastBlock)
        {
            if (DataStatisticsDac.Default.Height < lastBlock.Header.Height)
            {
                var start = DataStatisticsDac.Default.Height + 1;

                List<long> hs = new List<long>();
                object obj = new object();

                var left = Console.CursorLeft;
                var top = Console.CursorTop;

                for (long i = start; i <= lastBlock.Header.Height; i++)
                {
                    Monitor.Enter(obj);
                    hs.Add(i);
                    Monitor.Exit(obj);

                    var blockObj = BlockDac.Default.SelectByHeight(i);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(obj1 =>
                    {
                        var block = (BlockMsg)obj1;
                        var updateData = block.GetBlockUpdateData();
                        BlockDac.Default.UpdateExplorer(block, updateData);
                        Monitor.Enter(obj);
                        hs.Remove(block.Header.Height);
                        Console.SetCursorPosition(left, top);
                        Console.WriteLine($"Height[{block.Header.Height}]");
                        Monitor.Exit(obj);
                    }), blockObj);
                }
                while (hs.Count != 0)
                {
                    Thread.Sleep(1000 * 5);
                }
                DataStatisticsDac.Default.UpdateDb(lastBlock.Header.Height);
                BlockDac.Default.IsInitExplorerComplete = true;
            }
        }
    }
}