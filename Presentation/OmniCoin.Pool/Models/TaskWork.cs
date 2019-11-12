using OmniCoin.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniCoin.Pool.Models
{
    public class TaskWork
    {
        public static TaskWork Current = null;

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

        public void Add(Task item)
        {
            if (item == null)
                return;
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

        private void CyclicTask()
        {
            while (true)
            {
                try
                {
                    if (!(runningTaskInfo == null || runningTaskInfo.Task.IsCompleted || runningTaskInfo.Task.IsFaulted || runningTaskInfo.Task.IsCanceled))
                        continue;

                    if (!tasks.Any(x => x != null && x.Status == TaskStatus.Waiting))
                        continue;
                    if (runningTaskInfo != null)
                        runningTaskInfo.Status = TaskStatus.Completed;

                    var waittingTask = tasks.FirstOrDefault(x => x.Status == TaskStatus.Waiting);
                    waittingTask.Status = TaskStatus.Running;

                    this.runningTaskInfo = waittingTask;
                    waittingTask.Task.Start();

                    Task.WaitAll(waittingTask.Task);
                    tasks.RemoveAll(x => x.Status == TaskStatus.Canceded || x.Status == TaskStatus.Completed || runningTaskInfo.Task.IsFaulted);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex.ToString());
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
