using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KerbalWindTunnel.DataGenerators
{
    public class TaskProgressTracker
    {
        public Task Task
        {
            get => task; set
            {
                lock (this)
                {
                    if (task != null)
                        throw new InvalidOperationException("Cannot reassign to a new task.");
                    task = value;
                }
            }
        }
        private Task task = null;
        public int Total { get; private set; }
        public int Completed => progress;
        private int progress = 0;
        private Stopwatch stopwatch;
        public TaskProgressTracker FollowOnTaskTracker
        {
            get => followOnTracker;
            set
            {
                lock (this)
                    followOnTracker = value;
            }
        }
        private TaskProgressTracker followOnTracker = null;

        public float Progress => (float)Completed / Total;

        public TaskProgressTracker() { }
        public TaskProgressTracker(Task task)
        {
            this.task = task;
        }
        public Task LastFollowOnTask
        {
            get
            {
                if (followOnTracker == null)
                    return task;
                return followOnTracker.LastFollowOnTask;
            }
        }

        public void Start()
        {
            if (task == null)
                throw new ArgumentNullException(nameof(Task));
            stopwatch = Stopwatch.StartNew();
            if (task.Status == TaskStatus.WaitingForActivation)
                task.Start();
            task.ContinueWith(t => stopwatch.Stop());
        }

        public TimeSpan ElapsedTime => stopwatch?.Elapsed ?? TimeSpan.Zero;

        public TimeSpan EstimatedTimeRemaining
        {
            get
            {
                if (stopwatch == null)
                    return TimeSpan.Zero;
                if (Progress >= 1)
                    return TimeSpan.Zero;
                double invRemaining = 1 / Progress - 1;
                return TimeSpan.FromTicks((long)(stopwatch.Elapsed.Ticks * invRemaining));
            }
        }

        public void Increment() => Interlocked.Increment(ref progress);

        public void AmendTotal(int newTotal)
        {
            lock(this)
                Total = newTotal;
        }
    }
}
