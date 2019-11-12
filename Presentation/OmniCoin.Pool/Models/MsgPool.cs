


using OmniCoin.Framework;
using OmniCoin.Pool.Commands;
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
    internal class CommandState
    {
        internal PoolCommand Command;
        internal TcpReceiveState State;
    }

    internal class MsgPool
    {
        internal MsgPool()
        {
            commandInfos = new ConcurrentQueue<DataInfo>();
            this.Start();
        }

        internal static MsgPool Current;

        ConcurrentQueue<DataInfo> commandInfos = null;

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
                        DataInfo commandinfo;
                        if (commandInfos.TryDequeue(out commandinfo))
                        {
                            Task task = new Task(() =>
                            {
                                var command = DbHelper.Current.Get<PoolCommand>(DataType.CommandType, commandinfo.ID);
                                HeartbeatCommand.UpdateHeartTime(commandinfo.State);
                                PoolJob.TcpServer.ReceivedCommandAction(commandinfo.State, command);
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
            commandInfos.Clear();
        }

        internal void AddCommand(CommandState command)
        {
            if (command != null && isStart)
            {
                var id = DbHelper.Current.Put(DataType.CommandType, command.Command);
                commandInfos.Enqueue(new DataInfo { ID = id, State = command.State });
            }
        }
    }
}
