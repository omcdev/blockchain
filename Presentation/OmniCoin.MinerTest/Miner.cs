

// file LICENSE or http://www.opensource.org/licenses/mit-license.php.
using OmniCoin.Consensus;
using OmniCoin.Framework;
using OmniCoin.PoolMessages;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OmniCoin.MinerTest
{
    class Miner : IDisposable
    {
        public string PoolServerAddress { get; set; }
        public int PoolServerPort { get; set; }
        public string SerialNo { get; set; }
        public EnumMinerType MinerType { get; set; }
        public string WalletAddress { get; set; }
        public string PlotFilePath { get; set; }
        public long Capacity { get; set; }
        public long TotalCountofNonce { get; set; }

        private SocketClient2 socketClient;
        private int nonceSize = 64 * 4096;//256KB
        private int noncePerFile = 100;
        private int scoopDataLen = 64;
        private bool isInMinging = false;
        private bool isRegist = false;

        private Timer HeartTimer = null;

        public void Init(bool _isRegist = false)
        {
            IPAddress ip;            
            if (!IPAddress.TryParse(this.PoolServerAddress, out ip))
            {
                try
                {
                    var ips = Dns.GetHostAddresses(this.PoolServerAddress);

                    if (ips.Length > 0)
                    {
                        ip = ips[0];
                    }
                    else
                    {
                        throw new CommonException(ErrorCode.Engine.P2P.Connection.HOST_NAME_CAN_NOT_RESOLVED_TO_IP_ADDRESS);
                    }
                }
                catch
                {
                    throw new CommonException(ErrorCode.Engine.P2P.Connection.HOST_NAME_CAN_NOT_RESOLVED_TO_IP_ADDRESS);
                }
            }

            socketClient = new SocketClient2(new IPEndPoint(ip, this.PoolServerPort), Int16.MaxValue);
            socketClient.ConnectStatusChangedAction = ConnectStatusChanged;
            isRegist = _isRegist;
            socketClient.ReceivedCommandAction = ReceivedCommand;
            socketClient.ProcessErrorAction = SocketErroreceived;
        }

        public void Start()
        {
            socketClient.Connect();
            HeartTimer = new Timer();
            HeartTimer.Elapsed += HeartTimer_Elapsed;
            HeartTimer.Interval = 3000;
            HeartTimer.Start();
        }

        private void HeartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //SendHeartbeatCommand();
        }

        public void Stop()
        {
            socketClient.Disconnect();
        }

        public void InitPlotFiles()
        {
            TotalCountofNonce = Capacity / nonceSize;

            var origRow = Console.CursorTop;
            var origCol = Console.CursorLeft;

            var startIndex = GetScoopStartIndex();

            if (startIndex < 0)
            {
                Console.SetCursorPosition(origCol, origRow);
                Console.Write("Nonce Progress: " + TotalCountofNonce.ToString() + " / " + TotalCountofNonce.ToString());
            }
            var groups = (int)Math.Ceiling((double)(TotalCountofNonce) / (double)noncePerFile);
            long finishedNonce = startIndex-1;
            long maxFiles = groups * 4096L;

            var startG = startIndex / noncePerFile;

            for (int g = startG; g < groups; g++)
            {
                var min = g * (long)noncePerFile;
                var max = (g + 1) * (long)noncePerFile - 1L;

                if (max >= TotalCountofNonce)
                {
                    max = TotalCountofNonce - 1;
                }

                var nonceData = new ConcurrentBag<NonceData>();

                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - Nonce calculation started. Min: " + min.ToString() + " Max: " + max.ToString());
                Parallel.For(min, max, i => {
                    var data = POC.GenerateNonceData(WalletAddress, i);
                    nonceData.Add(data);
                    finishedNonce++;
                });
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " - Nonce calculation finished.");
                //for (long i = min; i <= max; i++)
                //{
                //    var data = POC.GenerateNonceData(WalletAddress, i);
                //    nonceData.Add(data);
                //    finishedNonce++;

                //    Console.SetCursorPosition(origCol, origRow);
                //    Console.Write("Nonce Progress: " + (finishedNonce + 1).ToString().PadLeft((TotalCountofNonce - 1).ToString().Length, '0') + " / " + TotalCountofNonce.ToString());
                //}

                Console.WriteLine("Nonce Progress: " + (finishedNonce + 1).ToString().PadLeft((TotalCountofNonce - 1).ToString().Length, '0') + " / " + TotalCountofNonce.ToString());

                var copyData = nonceData.OrderBy(n=>n.Nonce).ToArray();
                Task.Factory.StartNew(() => this.SaveNonce(PlotFilePath, WalletAddress, copyData, max, (TotalCountofNonce - 1).ToString().Length));
            }

            Console.WriteLine();
            Console.WriteLine("Init finished " + DateTime.Now.ToString());
            System.Threading.Thread.Sleep(5000);
        }

        public int GetScoopStartIndex()
        {
            int startScoop = 0;
            if (!Directory.Exists(PlotFilePath))
                return startScoop;

            var scoopDirs = Directory.GetDirectories(PlotFilePath);
            if (!scoopDirs.Any())
                return startScoop;
            var files = Directory.GetFiles(PlotFilePath + "/Scoop_4095");
            string fileName = null;
            if (files != null)
            {
                Array.Sort(files);
                fileName = files.LastOrDefault();
            }
            if (string.IsNullOrEmpty(fileName))
                return startScoop;
            //Console.WriteLine("fileName:" + fileName);
            var fs = Path.GetFileNameWithoutExtension(fileName).Split('_');
            var minNonce = int.Parse(fs[2]);
            var maxNonce = int.Parse(fs[3]);

            for (int i = 4095; i <= 0; i--)
            {
                var dirPath = Path.Combine(PlotFilePath, "Scoop_" + i);
                if (!Directory.Exists(PlotFilePath) || !Directory.Exists(dirPath))
                {
                    return minNonce;
                }
                var noncefiles = Directory.GetFiles(WalletAddress + "_Nonce" + "_*");
                if (noncefiles == null || !noncefiles.Any(x => x.Equals(fileName)))
                {
                    return minNonce;
                }
            }

            if (maxNonce == TotalCountofNonce)
                return -1;

            if ((maxNonce + 1) % noncePerFile > 0)
                startScoop = minNonce;
            else
                startScoop = maxNonce + 1;
            return startScoop;
        }

        private void SaveNonce(string location, string address, NonceData[] nonceData, long maxNonce, int endNonceLen)
        {
            System.Threading.Thread.CurrentThread.IsBackground = false;
            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }
            
            var minNonceText = ((maxNonce / noncePerFile) * noncePerFile).ToString().PadLeft(endNonceLen, '0');
            var maxNonceText = maxNonce.ToString().PadLeft(endNonceLen, '0');
            var fileName = string.Format("{0}_Nonce_{1}_{2}",address, minNonceText.PadLeft(4, '0'), maxNonceText.PadLeft(4, '0'));

            Parallel.For(0, 4096, i =>
            {
                var dirName = Path.Combine(PlotFilePath, "Scoop_" + i);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                var path = Path.Combine(dirName, fileName);
                using (var file = new FileStream(path, FileMode.Create))
                {
                    var scoopDatas = nonceData.SelectMany(x => x.DataList[i].FullData).ToArray();

                    file.Write(scoopDatas, 0, scoopDatas.Length);

                    file.Flush();
                }
            });

            //for (int i = 0; i < 4096; i++)
            //{
            //    var dirName = Path.Combine(PlotFilePath, "Scoop_" + i);
            //    if (!Directory.Exists(dirName))
            //    {
            //        Directory.CreateDirectory(dirName);
            //    }
            //    var path = Path.Combine(dirName, fileName);
            //    using (var file = new FileStream(path, FileMode.Create))
            //    {
            //        foreach (var nonce in nonceData)
            //        {
            //            var scoop = nonce.DataList.Where(d => d.Index == i).First();
            //            file.Write(scoop.FullData, 0, scoop.FullData.Length);
            //        }

            //        file.Flush();
            //    }
            //}
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(this);
            var path = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "OmniCoin.Miner.conf.json");

            using (var fileStream = File.Open(path, FileMode.Create))
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                fileStream.Write(bytes, 0, bytes.Length);

                fileStream.Flush();
            }
        }

        public static Miner LoadFromSetting()
        {
            var path = Path.Combine(Path.GetDirectoryName(typeof(Miner).Assembly.Location), "OmniCoin.Miner.conf.json");
            var json = File.ReadAllText(path);

            var miner = JsonConvert.DeserializeObject<Miner>(json);
            return miner;
        }

        private async void StartSendScoopData(StartMsg startMsg)
        {
            var scoop = startMsg.ScoopNumber.ToString();

            byte[] currentBytes = new byte[scoopDataLen];
            var scoopDir = Path.Combine(PlotFilePath, "Scoop_" + scoop);
            var files = Directory.GetFiles(scoopDir, WalletAddress + "_Nonce" + "_*");
            Array.Sort(files);
            int currentNonce = -1;

            foreach (var file in files)
            {
                var fileData = Path.GetFileNameWithoutExtension(file).Split('_');
                var minNonce = int.Parse(fileData[2]);
                var maxNonce = int.Parse(fileData[3]);

                var bytes = File.ReadAllBytes(file);
                var index = 0;
                var bytesLen = bytes.Length;
                currentNonce = minNonce;

                while (index < bytesLen && isInMinging)
                {
                    await Task.Run(() =>
                    {
                        currentBytes = new byte[scoopDataLen];
                        Array.Copy(bytes, index, currentBytes, 0, currentBytes.Length);

                        List<byte> targetByteLists = new List<byte>();
                        targetByteLists.AddRange(currentBytes);
                        targetByteLists.AddRange(startMsg.GenHash);
                        var baseTarget = Sha3Helper.Hash(targetByteLists.ToArray());

                        this.SendScoopDataCommand(startMsg.BlockHeight, currentNonce, startMsg.ScoopNumber, baseTarget);

                        Console.WriteLine("Progress: " + currentNonce.ToString().PadLeft((TotalCountofNonce - 1).ToString().Length, '0') + " / " + (TotalCountofNonce - 1).ToString());
                    });

                    Task.Delay(1).Wait();
                    index += scoopDataLen;
                    currentNonce += 1;
                }
                if (!isInMinging)
                {
                    LogHelper.Info("Mining is stopped");
                    return;
                }
            }

            Console.WriteLine();
            Console.WriteLine("All Nonces have been sent");
        }

        private void ConnectStatusChanged(bool connected)
        {
            if (connected && !isRegist)
            {
                LogHelper.Info("Connect successed, start to login");
                this.SendLoginCommand();
            }
            else
            {
                LogHelper.Info("Connect to MiningPool Fail , Wait ReConnect");

                Task.Delay(3000).Wait();

                Program.StartMining(this);
            }
        }

        private void SocketErroreceived(int errorCode)
        {
            this.isInMinging = false;
            LogHelper.Error("Socket error received " + ((SocketError)errorCode).ToString());
        }

        private void ReceivedCommand(PoolCommand cmd)
        {
            //LogHelper.Info("Received " + cmd.CommandName + " Command");
            switch(cmd.CommandName)
            {
                case CommandNames.MaxNonce:
                    this.ReceivedMaxNonceCommand(cmd.Payload);
                    break;
                case CommandNames.LoginResult:
                    this.ReceivedLoginResultCommand(cmd.Payload);
                    break;
                case CommandNames.Start:
                    this.ReceivedStartCommand(cmd.Payload);
                    break;
                case CommandNames.Stop:
                    this.ReceivedStopCommand(cmd.Payload);
                    break;
                case CommandNames.Reward:
                    this.ReceivedRewaradCommand(cmd.Payload);
                    break;
                case CommandNames.Reject:
                    this.ReceivedRejectCommand(cmd.Payload);
                    break;
                default:
                    break;
            }
        }

        private void ReceivedMaxNonceCommand(byte[] payload)
        {
            var msg = new MaxNonceMsg();
            int index = 0;
            msg.Deserialize(payload, ref index);

            this.SendNonceDataCommand(msg.RandomScoopNumber);
        }

        private void ReceivedLoginResultCommand(byte[] payload)
        {
            LogHelper.Info("Login successed, waiting for mining task.");
            HeartTimer.Start();
        }

        private void ReceivedRegistResultCommand(byte[] payload)
        {
            var msg = new RegistResultMsg();
            int index = 0;
            msg.Deserialize(payload, ref index);
            LogHelper.Info(string.Format("Regist Result : {0}", msg.Result ? "Success" : "Failure"));
        }

        private void ReceivedStartCommand(byte[] payload)
        {
            try
            {
                var msg = new StartMsg();
                int index = 0;
                msg.Deserialize(payload, ref index);

                LogHelper.Info("Received new block " + msg.BlockHeight + " mining task");
                this.isInMinging = true;
                this.StartSendScoopData(msg);
            }
            catch (Exception ex)
            {
                LogHelper.Error("ReceivedStartCommand handle error", ex);
            }
        }

        private void ReceivedStopCommand(byte[] payload)
        {
            var msg = new StopMsg();
            int index = 0;
            msg.Deserialize(payload, ref index);

            this.isInMinging = false;
            Console.WriteLine(Environment.NewLine);
            LogHelper.Info(string.Format("Block {0} mining task start at {1} and stop at {2}. Mining result is {3}",
                msg.BlockHeight, msg.StartTime, msg.StopTime, msg.Result));
        }

        private void ReceivedRewaradCommand(byte[] payload)
        {
            var msg = new RewardMsg();
            int index = 0;
            msg.Deserialize(payload, ref index);

            LogHelper.Info(string.Format("New block mining reward received:\nBlock Height:{0}\n\tMiner Hashes:{1}\n\tTotal Hashes:{2}\n\tAssigned Reward:{3}",
                msg.BlockHeight, msg.MinerHashes, msg.TotalHashes, msg.MinerReward));
        }

        private void ReceivedRejectCommand(byte[] payload)
        {
            LogHelper.Info("Previous command has been rejected by pool server");
        }

        private void SendHeartbeatCommand()
        {
            try
            {
                var cmd = PoolCommand.CreateCommand(CommandNames.Heartbeat, null);
                this.socketClient.SendCommand(cmd);
            }
            catch
            {

            }
        }

        private void SendLoginCommand()
        {
            var msg = new LoginMsg();
            msg.WalletAddress = this.WalletAddress;
            msg.MinerType = this.MinerType;
            msg.SerialNo = this.SerialNo;

            var cmd = PoolCommand.CreateCommand(CommandNames.Login, msg);
            this.socketClient.SendCommand(cmd);
        }

        public void SendRegistCommand(string walletAddress, string serialNo,string name)
        {
            var msg = new RegistMsg( );
            msg.WalletAddress = walletAddress;
            msg.SerialNo = serialNo;
            msg.Name = name;

            var cmd = PoolCommand.CreateCommand(CommandNames.Regist, msg);
            this.socketClient.SendCommand(cmd);
        }

        private void SendNonceDataCommand(int scoopNumber)
        {
            var maxNonce = this.TotalCountofNonce - 1;
            var minNonce = (maxNonce / noncePerFile) * noncePerFile;

            string maxNonceText = maxNonce.ToString().PadLeft(4, '0');
            string minNonceText = minNonce.ToString().PadLeft(4, '0');

            string fileName = string.Format("Scoop_{0}/{1}_Nonce_{2}_{3}", scoopNumber, WalletAddress, minNonceText, maxNonceText);
            //var fileName = this.WalletAddress + "_" + scoopNumber.ToString().PadLeft(4, '0') + "_" + minNonceText + "_" + maxNonceText;
            var filePath = Path.Combine(PlotFilePath, fileName);

            var bytes = File.ReadAllBytes(filePath);
            var scoopData = new byte[64];
            Array.Copy(bytes, bytes.Length - scoopData.Length, scoopData, 0, scoopData.Length);

            var msg = new NonceDataMsg();
            msg.MaxNonce = maxNonce;
            msg.ScoopData = scoopData;

            var cmd = PoolCommand.CreateCommand(CommandNames.NonceData, msg);
            this.socketClient.SendCommand(cmd);
        }

        private void SendScoopDataCommand(long height, long nonce, int scoopNumber, byte[] target)
        {
            var msg = new ScoopDataMsg();
            msg.BlockHeight = height;
            msg.WalletAddress = WalletAddress;
            msg.Nonce = nonce;
            msg.ScoopNumber = scoopNumber;
            msg.Target = target;

            var cmd = PoolCommand.CreateCommand(CommandNames.ScoopData, msg);
            this.socketClient.SendCommand(cmd);
        }

        public void Dispose()
        {
            socketClient = null;
        }
    }
}
