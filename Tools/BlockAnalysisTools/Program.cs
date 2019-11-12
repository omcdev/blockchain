using OmniCoin.DTO.Explorer;
using System;
using System.Collections.Generic;
using System.Linq;
using static BlockAnalysisTools.RpcParam;

namespace BlockAnalysisTools
{
    class Program
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            RpcBLL.UrlString = "http://127.0.0.1:58804";
            long currentMaxHeight = -1;
            long lastAnalysisHeight = -1;
            lastAnalysisHeight++;
            currentMaxHeight = RpcBLL.GetBlockCount();
            if (currentMaxHeight > -1)
            {
                for(var i = lastAnalysisHeight;i<= currentMaxHeight; i++)
                {                    
                    var blockHash = RpcBLL.GetBlockHashByHeight(i);
                    if (string.IsNullOrWhiteSpace(blockHash))
                    {
                        logger.Warn($"Height 【{i}】 's blockHash is null or emtpy");
                        continue;
                    }
                    var block = RpcBLL.GetBlockInfo(blockHash);
                    if(block != null)
                    {
                        BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.Height_BlockHash, i.ToString()), blockHash);
                        BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.BlockHash_Height, blockHash), i.ToString());
                        BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.Height_BlockData, i.ToString()), block);

                        var day = LongToDatetime(block.Timestamp);

                        var Day_Blocks = BlockAnalysisDb.DB.Get<List<long>>(BlockAnalysisDb.GetKey(LevelDBType.Day_Blocks,day));
                        if(Day_Blocks == null)
                        {
                            Day_Blocks = new List<long>();
                        }
                        Day_Blocks.Add(block.Height);                        
                        BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.Day_Blocks, day), Day_Blocks);

                        string Day_Blocks_Counts_String = BlockAnalysisDb.DB.Get(BlockAnalysisDb.GetKey(LevelDBType.Day_Blocks_Counts,day));
                        long Day_Blocks_Counts = 0;
                        long.TryParse(Day_Blocks_Counts_String, out Day_Blocks_Counts);
                        Day_Blocks_Counts++;
                        BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.Day_Blocks_Counts, day), Day_Blocks_Counts.ToString());


                        if (block.TranList != null && block.TranList.Any())
                        {
                            var Day_TxHashs = BlockAnalysisDb.DB.Get<List<string>>(BlockAnalysisDb.GetKey(LevelDBType.Day_TxHashs, day));
                            if (Day_TxHashs == null)
                            {
                                Day_TxHashs = new List<string>();
                            }
                            Day_TxHashs.AddRange(block.TranList.Select(x => x.Hash));
                            BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.Day_TxHashs, day), Day_TxHashs);


                            string Day_TxHashs_Counts_String = BlockAnalysisDb.DB.Get(BlockAnalysisDb.GetKey(LevelDBType.Day_TxHashs_Counts, day));
                            long Day_TxHashs_Counts = 0;
                            long.TryParse(Day_TxHashs_Counts_String, out Day_TxHashs_Counts);
                            Day_TxHashs_Counts++;
                            BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.Day_TxHashs_Counts, day), Day_TxHashs_Counts.ToString());

                            block.TranList.ForEach(x =>
                            {
                                var tx = BlockAnalysisDb.DB.Get<TransactionDetailOM>(BlockAnalysisDb.GetKey(LevelDBType.TxHash, x.Hash));
                                if (tx != null)
                                {
                                    logger.Warn($"Height 【{block.Height}】,Tx 【{x.Hash}】 已存在 Leveldb中");                                    
                                }
                                else
                                {
                                    BlockAnalysisDb.DB.Put(BlockAnalysisDb.GetKey(LevelDBType.TxHash, x.Hash), x);
                                }
                                
                            });
                            if(block.TradeCount != block.TranList.Count)
                            {
                                logger.Warn($"Height 【{i}】- BlockHash 【{blockHash}】 block.TradeCount 【{block.TradeCount}】 != block.TranList.Count 【{block.TranList.Count}】");
                            }
                        }

                    }
                    else
                    {
                        logger.Warn($"Height 【{i}】- BlockHash 【{blockHash}】 's BlockInfo is null");                        
                    }
                    logger.Info($"currentMaxHeight : { currentMaxHeight }, CurrentProcessedHeight : {i}");
                }
            }
            Console.WriteLine("World Game Over");

            Console.ReadLine();

        }

        public static string LongToDatetime(long timestamp)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            long tt = dt.Ticks + timestamp * 10000;
            return new DateTime(tt).ToString("yyyyMMdd");
        }
    }
}
