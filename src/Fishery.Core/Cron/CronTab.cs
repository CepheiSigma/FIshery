using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using Fishery.Core.Extension;
using Fishery.Core.Utils;
using NCrontab;

namespace Fishery.Core.Cron
{
    public class TimeConfig
    {
        public List<int> Second { get; set; }
        public List<int> Minute { get; set; }
        public List<int> Hour { get; set; }
        public List<int> Day { get; set; }
        public List<int> Month { get; set; }
        public List<int> Week { get; set; }
    }

    public class TaskConfig : SharedObject
    {
        public TimeConfig TimeLine;
        public ScheduleType TargetType;
        public string CronConfig;
        public CrontabSchedule CrontabSchedule;
        public DateTime? NextDateTime;
        public DateTime LastRunTime;
        public List<TaskThread> ThreadPool;
        public Guid Handle;

        public TaskConfig(string expression)
        {
            CronConfig = expression;
            CrontabSchedule = CrontabSchedule.Parse(expression);
            NextDateTime = CrontabSchedule.GetNextOccurrence(DateTime.Now);
            ThreadPool = new List<TaskThread>();
            Handle = Guid.NewGuid();
        }

        public void GoNext()
        {
            NextDateTime = CrontabSchedule.GetNextOccurrence(DateTime.Now);
        }
    }

    public class TaskThread : SharedObject
    {
        private TimeSpan _maxAliveTime;
        private string _executionId;
        public string ExecutionId => _executionId;

        public TaskThread(Thread thread, TimeSpan maxAliveTime,string executionId)
        {
            _maxAliveTime = maxAliveTime;
            _executionId = executionId;
            Thread = thread;
            Thread.Start();
        }

        public DateTime CreateTime { get; set; }
        public Thread Thread { get; }

        public bool IsAlive
        {
            get
            {
                if (CreateTime + _maxAliveTime < DateTime.Now)
                {
                    Thread.Abort();
                    EventRouter.GetInstance().FireEvent("Task_Timeout",this,_executionId);
                }
                return Thread.IsAlive;
            }
        }
    }

    public delegate void OnToggleTask(string executionId);

    public class ScheduleType : SharedObject
    {
        public OnToggleTask Method { get; set; }
        public bool IsAsync { get; set; }
        public string Name => Method.Method.Name;
        public TimeSpan MaximumExecuteTime { get; set; }

        public ScheduleType(OnToggleTask method, bool async)
        {
            IsAsync = async;
            Method = method;
            MaximumExecuteTime = TimeSpan.FromHours(24);
        }

        public TaskThread Run(string executionId)
        {
            TaskThread taskThread = new TaskThread(new Thread((o) =>
            {
                try
                {
                    Method.Invoke(executionId);
                }
                catch (Exception ex)
                {
                    EventRouter.GetInstance().FireEvent("Warn_Occurred", this,
                        $"Execute task {Method} failed: {(ex.InnerException == null ? ex.Message : ex.InnerException.Message)}\nStack trace:\n{ex.StackTrace}");
                }
            }), MaximumExecuteTime, executionId) {CreateTime = DateTime.Now};
            return taskThread;
        }
    }

    public class CronTab : SharedObject
    {
        private readonly List<TaskConfig> _taskConfig;
        private static CronTab _instance;
        private bool _isWakeUp;

        private CronTab()
        {
            EventRouter.GetInstance().ListenTo("Clock_Tick", new CallBackDelegate(WakeUp), true);
            _taskConfig = new List<TaskConfig>();
            _isWakeUp = false;
        }

        public void WakeUp(object sender, object eventArgs)
        {
            DateTime now = DateTime.Now;
            if (_isWakeUp)
                return;
            _isWakeUp = true;
            try
            {
                foreach (var taskConfig in _taskConfig)
                {
                    bool isAllComplete = taskConfig.ThreadPool.TrueForAll(item => !item.IsAlive);
                    if (isAllComplete)
                        taskConfig.ThreadPool.Clear();
                    if (taskConfig.NextDateTime.HasValue && taskConfig.NextDateTime <= DateTime.Now)
                    {
                        string executionId = Common.MD5(Clock.GetTimeStamp(true) + taskConfig.TargetType.Name);
                        if (taskConfig.ThreadPool.Count > 0 && !taskConfig.TargetType.IsAsync)
                            continue;
                        taskConfig.LastRunTime = now;
                        TaskThread thread = taskConfig.TargetType.Run(executionId);
                        taskConfig.ThreadPool.Add(thread);
                        taskConfig.GoNext();
                    }
                }
            }
            catch (Exception ex)
            {
                EventRouter.GetInstance().FireEvent("Error_Occurred", "CronTab",
                    "Fatal error:" + ex.GetBaseException().Message + "\n\t" + ex.GetBaseException().StackTrace);
            }
            finally
            {
                _isWakeUp = false;
            }
        }

        public Guid RegisterTask(ScheduleType type, string cronExpression)
        {
            if (type != null)
            {
                TaskConfig taskConfig = new TaskConfig(cronExpression)
                {
                    TargetType = type
                };
                _taskConfig.Add(taskConfig);
                return taskConfig.Handle;
            }

            return Guid.Empty;
        }

        public void UnregisterTask(Guid handle)
        {
            int index = _taskConfig.FindIndex(callback => callback.Handle.Equals(handle));
            if (index >= 0)
                _taskConfig.RemoveAt(index);
        }

        public static CronTab GetInstance(CronTab cronTab = null)
        {
            return _instance = _instance ?? cronTab ?? new CronTab();
        }
    }
}