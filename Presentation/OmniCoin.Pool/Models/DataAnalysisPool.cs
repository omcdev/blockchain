


using OmniCoin.Framework;
using OmniCoin.Pool.Helpers;
using OmniCoin.Pool.Sockets;
using OmniCoin.PoolMessages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OmniCoin.Pool.Models
{
    internal class AnalysisData
    {
        internal TcpReceiveState State;
        internal byte[] Data;
    }

    internal class DataInfo
    {
        internal TcpReceiveState State;
        internal long ID;
    }

    internal class DataAnalysisPool
    {
        internal DataAnalysisPool()
        {
            analysisDataIds = new ConcurrentQueue<DataInfo>();
            this.Start();
        }

        public static DataAnalysisPool Current;

        ConcurrentQueue<DataInfo> analysisDataIds = null;

        bool isStart = false;

        SafeCollection<Task> RunningTasks = new SafeCollection<Task>();

        const int MaxTaskCount = 100;

        internal void Start()
        {
            isStart = true;
            Task.Run(() =>
            {
                while (isStart)
                {
                    if (RunningTasks.Count < MaxTaskCount)
                    {
                        DataInfo analysisDataInfo;
                        if (analysisDataIds.TryDequeue(out analysisDataInfo))
                        {
                            Task task = new Task(() =>
                            {
                                var analysisData = DbHelper.Current.Get<byte[]>(DataType.ReceiveType, analysisDataInfo.ID);
                                var state = analysisDataInfo.State;
                                var buffer = analysisData;
                                var commandDataList = new List<byte[]>();
                                var index = 0;
                                List<byte> bytes = null;

                                while (index < buffer.Length)
                                {
                                    if (bytes == null)
                                    {
                                        if ((index + 3) < buffer.Length &&
                                        buffer[index] == PoolCommand.DefaultPrefixBytes[0] &&
                                        buffer[index + 1] == PoolCommand.DefaultPrefixBytes[1] &&
                                        buffer[index + 2] == PoolCommand.DefaultPrefixBytes[2] &&
                                        buffer[index + 3] == PoolCommand.DefaultPrefixBytes[3])
                                        {
                                            bytes = new List<byte>();
                                            bytes.AddRange(PoolCommand.DefaultPrefixBytes);
                                            index += 4;
                                        }
                                        else
                                        {
                                            index++;
                                        }
                                    }
                                    else
                                    {
                                        if ((index + 3) < buffer.Length &&
                                        buffer[index] == PoolCommand.DefaultSuffixBytes[0] &&
                                        buffer[index + 1] == PoolCommand.DefaultSuffixBytes[1] &&
                                        buffer[index + 2] == PoolCommand.DefaultSuffixBytes[2] &&
                                        buffer[index + 3] == PoolCommand.DefaultSuffixBytes[3])
                                        {
                                            bytes.AddRange(PoolCommand.DefaultSuffixBytes);
                                            commandDataList.Add(bytes.ToArray());
                                            bytes = null;

                                            index += 4;
                                        }
                                        else
                                        {
                                            bytes.Add(buffer[index]);
                                            index++;
                                        }
                                    }
                                }
                                
                                foreach (var data in commandDataList)
                                {
                                    try
                                    {
                                        var cmd = PoolCommand.ConvertBytesToMessage(data);
                                        if (cmd != null)
                                        {
                                            MsgPool.Current.AddCommand(new CommandState { State = state, Command = cmd });
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogHelper.Error("Error occured on deserialize messgae: " + ex.Message, ex);
                                    }
                                }
                            });
                            task.ContinueWith(t =>
                            {
                                RunningTasks.Remove(task);
                            });
                            RunningTasks.Add(task);
                            task.Start();
                        }
                    }
                }
            });
        }

        internal void Stop()
        {
            isStart = false;
            analysisDataIds.Clear();
        }

        internal void AddData(AnalysisData analysisData)
        {
            if (analysisData != null && isStart)
            {
                var id = DbHelper.Current.Put(DataType.ReceiveType, analysisData.Data);
                analysisDataIds.Enqueue(new DataInfo { State = analysisData.State, ID = id });
            }
        }
    }
}
