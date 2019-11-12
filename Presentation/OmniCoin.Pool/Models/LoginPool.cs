using OmniCoin.Framework;
using OmniCoin.Pool.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCoin.Pool.Models
{
    public class LoginPool
    {
        internal LoginPool()
        {
            loginCommands = new ConcurrentQueue<Task>();
            this.Start();
        }

        internal static LoginPool Current;

        ConcurrentQueue<Task> loginCommands = null;

        bool isStart = false;

        SafeCollection<Task> RunningTasks = new SafeCollection<Task>();

        const int MaxTaskCount = 2;

        internal void Start()
        {
            isStart = true;
            Task.Run(() =>
            {
                while (isStart)
                {
                    if (RunningTasks.Count < MaxTaskCount)
                    {
                        Task command;
                        if (loginCommands.TryDequeue(out command))
                        {
                            command.ContinueWith(t =>
                            {
                                RunningTasks.Remove(command);
                            });
                            RunningTasks.Add(command);
                            command.Start();
                        }
                    }
                }
            });
        }

        internal void Stop()
        {
            isStart = false;
            loginCommands.Clear();
        }

        internal void AddCommand(Task task)
        {
            if (task != null && isStart)
                loginCommands.Enqueue(task);
        }

    }

    //internal class TaskInfo
    //{
    //    public TaskStatus Status { get; set; }
    //    public Task Task { get; set; }
    //}

    //internal enum TaskStatus
    //{
    //    Waiting,
    //    Running,
    //    Canceded,
    //    Completed
    //}
}
