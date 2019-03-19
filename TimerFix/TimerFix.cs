using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SecretNest.TimerFix
{
    /// <summary>
    /// A replacement of NetFx Threading.Timer without cumulative error.
    /// </summary>
    public class TimerFix : IDisposable
    {
        Task waitingJob;
        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;

        object state;
        TimeSpan period;
        DateTime next;
        TimerCallback timerCallback;

        /// <summary>
        /// Initializes a new instance of the TimerFix class with an infinite period and an infinite due time, using the newly created Timer object as the state object.
        /// </summary>
        /// <param name="timerCallback">A TimerCallback delegate representing a method to be executed.</param>
        public TimerFix(TimerCallback timerCallback)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
        }

        /// <summary>
        /// Initializes a new instance of the TimerFix class, using a 32-bit signed integer to specify the time interval.
        /// </summary>
        /// <param name="timerCallback">A TimerCallback delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before callback is invoked, in milliseconds. Specify Infinite to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of callback, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        public TimerFix(TimerCallback timerCallback, object state, int dueTime, int period)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (period <= 0 && period != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            this.state = state;
            StartJob(dueTime, new TimeSpan(0, 0, 0, 0, period));
        }

        /// <summary>
        /// Initializes a new instance of the TimerFix class, using TimeSpan values to measure time intervals.
        /// </summary>
        /// <param name="timerCallback">A delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before the callback parameter invokes its methods. Specify negative one (-1) milliseconds to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the methods referenced by callback. Specify negative one (-1) milliseconds to disable periodic signaling.</param>
        public TimerFix(TimerCallback timerCallback, object state, TimeSpan dueTime, TimeSpan period)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (period <= TimeSpan.Zero && period.TotalMilliseconds != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            this.state = state;
            StartJob(dueTime, period);
        }

            
        void StartJob(TimeSpan dueTime, TimeSpan period)
        {
            next = DateTime.Now.Add(dueTime);
            StartJobWithNoDueTimeSet(period);
        }

        void StartJob(int dueTime, TimeSpan period)
        {
            next = DateTime.Now.AddMilliseconds(dueTime);
            StartJobWithNoDueTimeSet(period);
        }

        void StartJobWithNoDueTimeSet(TimeSpan period)
        {
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            if (period.TotalMilliseconds == -1)
            {
                waitingJob = Task.Run(async () => await OnceJob());
            }
            else
            {
                this.period = period;
                waitingJob = Task.Run(async () => await Job());
            }
        }

        async Task Job()
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var delay = next - DateTime.Now;
                    if (delay > TimeSpan.Zero)
                    {
                        await Task.Delay(delay, cancellationToken);
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Run(() => timerCallback(state), cancellationToken);
                    }
                    next = next.Add(period);
                }
            }
            catch (TaskCanceledException) { }
            waitingJob = null;
        }

        async Task OnceJob()
        {
            var delay = next - DateTime.Now;

            try
            {
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Run(() => timerCallback(state), cancellationToken);
                }
            }
            catch (TaskCanceledException) { }
            waitingJob = null;
        }

        object stopLock = new object();
        void StopCurrentJob()
        {
            lock (stopLock)
            {
                if (waitingJob != null)
                {
                    cancellationTokenSource.Cancel();

                    while (waitingJob != null)
                    {
                        Task.Delay(100).Wait();
                    }

                    cancellationTokenSource = null;
                }
            }
        }

        /*
         * in Concurrent Programming on Windows p 373
         * Note that although Change is typed as returning a bool,
         * it will actually never return anything but true.
         * If there is a problem changing the timer-such as the target object
         * already having been deleted-an exception will be thrown.
         */

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 32-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the Timer was constructed, in milliseconds. Specify Infinite to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        /// <returns>true if the timer was successfully updated; otherwise, false. Actually, only true will be returned.</returns>
        public bool Change(int dueTime, int period)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (period <= 0 && period != -1)
                throw new ArgumentOutOfRangeException(nameof(period));

            StopCurrentJob();
            StartJob(dueTime, new TimeSpan(0, 0, 0, 0, period));

            return true;
        }

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using TimeSpan values to measure time intervals.
        /// </summary>
        /// <param name="dueTime">A TimeSpan representing the amount of time to delay before invoking the callback method specified when the Timer was constructed. Specify negative one (-1) milliseconds to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed. Specify negative one (-1) milliseconds to disable periodic signaling.</param>
        /// <returns>true if the timer was successfully updated; otherwise, false. Actually, only true will be returned.</returns>
        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (period <= TimeSpan.Zero && period.TotalMilliseconds != -1)
                throw new ArgumentOutOfRangeException(nameof(period));

            StopCurrentJob();
            StartJob(dueTime, period);

            return true;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopCurrentJob();
                }
                stopLock = null;
                timerCallback = null;
                disposedValue = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the current instance of TimerFix.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
#endregion
    }
}
