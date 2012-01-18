﻿using System;
using System.Linq;
using InlineScheduler.Advanced;
using System.Threading.Tasks;
using System.Threading;

namespace InlineScheduler
{
    public class Scheduler
    {
        private readonly WorkBag _work;
        private readonly WorkItemFactory _itemFactory;        
        private readonly DateTime _sartTime;

        private bool _stopped;

        private readonly Timer _timer;

        public Scheduler(ISchedulerContext context = null)
        {
            context = context ?? new DefaultSchedulerContext();
            _work = new WorkBag(context);
            _itemFactory = new WorkItemFactory(context);
            _stopped = true;
            _sartTime = DateTime.Now;
            
            // Start timer
            _timer = new Timer(OnTimerElapsed);
            _timer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(1000));

        }

        void OnTimerElapsed(object sender)  
        {
            _work.UpdateState();
            if (!_stopped)
            {
                var runningCount = _work.GetRuningWork();

                if (runningCount < 20)
                {
                    var applicableDefs = _work.GetApplicableToRun(20);

                    foreach (var def in applicableDefs)
                    {
                        def.Run();
                    }
                }
            }   
        }        
        
        public SchedulerStats GatherStats()
        {
            return new SchedulerStats
            {
                Overal = GatherOveralStats(),
                CurrentJobs = StatsHelper.GatherCurrentJobs(_work)
            };
        }

        public OveralStats GatherOveralStats()
        {
            var stats = StatsHelper.GatherOveralStatistics2(_work);
            stats.IsStopped = _stopped;
            stats.StartTime = _sartTime;
            return stats;            
        }

        public SchedulerJobStats GatherJobStats(string workKey)
        {
            return StatsHelper.GatherJobStats(_work, workKey);
        }

        public bool IsStopped { get { return _stopped; } }
        public bool IsRunningJobsNow
        {
            get
            {
                return GatherOveralStats().RunningJobs > 0;
            }
        }

        public void Stop() 
        {
            _stopped = true;
        }

        public void Start()
        {
            _work.UpdateState();
            _stopped = false;
        }

        public void Schedule(string workKey, Action work, TimeSpan interval, string description = null)
        {
            Func<Task> factory = () => Task.Factory.StartNew(work);
            Schedule(workKey, factory, interval, description);
        }
        
        public void Schedule(string workKey, Func<Task> factory, TimeSpan interval, string description = null)
        {
            if (!_work.IsWorkRegisterd(workKey))
            {
                var item = _itemFactory.Create(workKey, factory, interval, description);
                _work.Add(item);
            }
        }

        /// <summary>
        ///     Forces work item to run.
        /// </summary>
        public void Force(string workKey)
        {
            var def = _work.FirstOrDefault(x => x.WorkKey == workKey);
            if (def != null)
            {
                def.Force();
            }
        }        
    }
}
