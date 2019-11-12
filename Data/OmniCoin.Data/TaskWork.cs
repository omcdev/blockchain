// Copyright (c) 2018 FiiiLab Technology Ltd
// Distributed under the MIT software license, see the accompanying
// file LICENSE or or http://www.opensource.org/licenses/mit-license.php.
using FiiiChain.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FiiiChain.Data
{
    public class TaskWork
    {
        public TaskWork()
        {
            tasks = new List<TaskInfo>();
            Start();
        }

        private SafeCollection<TaskInfo> tasks;

        private TaskInfo runningTaskInfo = null;

        #region 基础操作

        public int Count => tasks.Count(x => x.Status != TaskStatus.Canceded);

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(Task item, string name = null)
        {
            if (item == null)
                return;

            //if (!string.IsNullOrEmpty(name))
            //{
            //    LogHelper.Warn($"前排任务数{tasks.Count()} 当前任务：{name}");
            //}

            TaskInfo taskInfo = new TaskInfo() { Status = TaskStatus.Waiting, Task = item };
            tasks.Add(taskInfo);
        }

        public void Clear()
        {
            tasks.Clear();
        }

        public bool Contains(Task item)
        {
            return tasks.Any(x => x.Status != TaskStatus.Canceded && x.Task == item);
        }

        public void Remove(Task item)
        {
            var task = tasks.FirstOrDefault(x => x.Status != TaskStatus.Canceded && x.Task == item);
            if (task != null)
            {
                task.Status = TaskStatus.Canceded;
            }
        }
        #endregion

        #region 任务自执行
        private void Start()
        {
            Task.Run(() =>
            {
                CyclicTask();
            });
        }

        bool isRunning = true;

        public void Stop()
        {
            isRunning = false;
            if (runningTaskInfo != null && runningTaskInfo.Status == TaskStatus.Running && runningTaskInfo.Task.Status == System.Threading.Tasks.TaskStatus.Running)
                Task.WaitAll(runningTaskInfo.Task);
        }

        public void ReStart()
        {
            isRunning = true;
        }

        private void CyclicTask()
        {
            while (true)
            {
                try
                {
                    if (!isRunning)
                    {
                        Task.Delay(1000).Wait();
                        continue;
                    }

                    if (!(runningTaskInfo == null || runningTaskInfo.Task.IsCompleted || runningTaskInfo.Task.IsFaulted || runningTaskInfo.Task.IsCanceled))
                    {
                        Task.Delay(10).Wait();
                        continue;
                    }

                    if (!tasks.Any(x => x != null && x.Status == TaskStatus.Waiting))
                    {
                        Task.Delay(10).Wait();
                        continue;
                    }
                    if (runningTaskInfo != null)
                        runningTaskInfo.Status = TaskStatus.Completed;

                    var waittingTask = tasks.FirstOrDefault(x => x.Status == TaskStatus.Waiting);
                    waittingTask.Status = TaskStatus.Running;

                    this.runningTaskInfo = waittingTask;
                    waittingTask.Task.Start();

                    Task.WaitAll(waittingTask.Task);
                    tasks.RemoveAll(x => x == null || x.Status == TaskStatus.Canceded || x.Status == TaskStatus.Completed || runningTaskInfo.Task.IsFaulted || x.Task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex.ToString());
                    tasks.RemoveAll(x => x == null || x.Status == TaskStatus.Canceded || x.Status == TaskStatus.Completed || runningTaskInfo.Task.IsFaulted || x.Task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion);
                }
            }
        }
        #endregion
    }

    internal class TaskInfo
    {
        public TaskStatus Status { get; set; }
        public Task Task { get; set; }
    }

    internal enum TaskStatus
    {
        Waiting,
        Running,
        Canceded,
        Completed
    }
}
