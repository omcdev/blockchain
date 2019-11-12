

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Framework;
using OmniCoin.PoolMessages;
using System;
using System.Threading.Tasks;

namespace OmniCoin.MinerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            GlobalParameters.IsTestnet = true;

            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    //case "register":
                    //    break;
                    case "init":
                        Init(args);
                        break;
                    case "start":
                        StartMining();
                        break;
                    //case "stop":
                    //    Stop(args);
                    //    break;
                    default:
                        UnknownCommand();
                        break;
                }
            }
            else
            {
                UnknownCommand();
            }
        }

        static void UnknownCommand()
        {
            Console.WriteLine("Useage: dotnet MinerTest.dll [command] [options]");
            Console.WriteLine("");
            Console.WriteLine("Supported Commands:");
            Console.WriteLine("     Register: register current device to mining pool");
            Console.WriteLine("     Init: initialize local devie, prepare to start mining");
            //Console.WriteLine("     Read: read local plot data");
            Console.WriteLine("     Start: start mining");
            //Console.WriteLine("     Stop: stop mining");
            Console.WriteLine("");
            Console.WriteLine("Run ' [command] --help' for more information about a command.");
        }

        static void Init(string[] args)
        {
            var miner = new Miner();

            try
            {
                miner.PoolServerAddress = args[1];
                miner.PoolServerPort = int.Parse(args[2]);
                miner.SerialNo = args[3];
                miner.MinerType = EnumMinerType.POS;
                miner.WalletAddress = args[4];
                miner.PlotFilePath = args[5];
                miner.Capacity = long.Parse(args[6]);
            }
            catch
            {
                Console.WriteLine("Useage: dotnet MinerTest.dll Init <PoolServerAddress> <PoolServerPort> <SerialNo> <WalletAddress> <PlotFilePath> <Capacity>");
                Console.WriteLine("");
                Console.WriteLine("Parameters:");
                Console.WriteLine("     PoolServerAddress: IP address of pool server");
                Console.WriteLine("     PoolServerPort: Tcp port of pool server");
                Console.WriteLine("     SerialNo: The Serial No of POS");
                Console.WriteLine("     WalletAddress: Miner's wallet address");
                Console.WriteLine("     PlotFilePath: The directory path used to storage plot files");
                Console.WriteLine("     Capacity: Max storage capacity used to storage plot files");
                Console.WriteLine("");
                return;
            }

            miner.InitPlotFiles();
            miner.SaveSettings();
            //var location = @"D:\Plot";
            //var capacity = 500 * 1024 * 1024; //500MB
            //var nonceSize = 64 * 4096;
            //var nonceNum = capacity / nonceSize;
            //var walletAddress = "omnitLkG3NM4FrXWFFLCB3FCD6c6SXiNqU3VcC";

            //var nonceData = new List<NonceData>();
            //Console.WriteLine("Start initialize storage " + DateTime.Now.ToString());
            //var origRow = Console.CursorTop;
            //var origCol = Console.CursorLeft;

            //for (int i = 0; i < nonceNum; i ++)
            //{
            //    var data = POC.GenerateNonceData(walletAddress, i);
            //    nonceData.Add(data);

            //    Console.SetCursorPosition(origCol, origRow);
            //    Console.Write("Progress: " + i.ToString().PadLeft(nonceNum.ToString().Length, '0') + " / " + nonceNum.ToString());

            //    if(nonceData.Count == 100 || i == (nonceNum - 1))
            //    {
            //        var copyData = nonceData.ToArray();
            //        var index = i;
            //        Task.Factory.StartNew(() => saveNonce(location, walletAddress, copyData, index, nonceNum.ToString().Length));
            //        nonceData.Clear();
            //    }
            //}

            //Console.WriteLine("Init finished " + DateTime.Now.ToString());
            //System.Threading.Thread.Sleep(5000);
        }

        public static void StartMining(Miner oldMiner = null)
        {
            try
            {
                if (oldMiner != null)
                {
                    oldMiner.Dispose();
                    oldMiner = null;
                }
                var miner = Miner.LoadFromSetting();
                miner.Init();
                miner.Start();
                Console.ReadLine();
            }
            catch(Exception ex)
            {
                LogHelper.Error(ex.ToString());
                Task.Delay(3000).Wait();
                StartMining();
            }
        }

        static void Stop(string[] args)
        {

        }
    }
}
