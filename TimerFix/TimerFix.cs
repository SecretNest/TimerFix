using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SecretNest.TimerFix
{
    public class TimerFix : IDisposable
    {
        Timer timer;
        object state;
        long periodTicks;
        TimeSpan interval;
        DateTime next;
        TimerCallback timerCallback;
        static TimeSpan disabled = new TimeSpan(0, 0, 0, 0, -1);
        SpinLock spinLock = new SpinLock();

        /// <summary>
        /// Initializes a new instance of the TimerFix class with an infinite period and an infinite due time, using the newly created Timer object as the state object.
        /// </summary>
        /// <param name="timerCallback">A TimerCallback delegate representing a method to be executed.</param>
        /// <param name="interval">The time, in milliseconds, between checking whether the period is passed. Default value is 15.</param>
        public TimerFix(TimerCallback timerCallback, int interval = 15)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval)); state = null;
            this.interval = new TimeSpan((long)interval * 10000);
            timer = new Timer(Job, null, disabled, disabled);
        }

        /// <summary>
        /// Initializes a new instance of the TimerFix class, using a 32-bit signed integer to specify the time interval.
        /// </summary>
        /// <param name="timerCallback">A TimerCallback delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before callback is invoked, in milliseconds. Specify Infinite to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of callback, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        /// <param name="interval">The time, in milliseconds, between checking whether the period is passed. Default value is 15.</param>
        public TimerFix(TimerCallback timerCallback, object state, int dueTime, int period, int interval = 15)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (period < 0 && period != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval));
            this.state = state;
            this.interval = new TimeSpan((long)interval * 10000);
            TimeSpan due = new TimeSpan(dueTime * 10000);
            if (period == -1)
            {
                next = DateTime.Now;
                timer = new Timer(Job, null, due, disabled);
            }
            else
            {
                periodTicks = period * 10000;
                next = DateTime.Now.AddTicks(periodTicks);
                timer = new Timer(Job, null, due, this.interval);
            }
        }

        /// <summary>
        /// Initializes a new instance of the TimerFix class, using 64-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="timerCallback">A TimerCallback delegate representing a method to be execute</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before callback is invoked, in milliseconds. Specify Infinite to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of callback, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        /// <param name="interval">The time, in milliseconds, between checking whether the period is passed. Default value is 15.</param>
        public TimerFix(TimerCallback timerCallback, object state, long dueTime, long period, long interval = 15)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (period < 0 && period != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            else if (period > 4294967294)
                throw new NotSupportedException(nameof(period) + " greater than 4294967294 is not supported.");
            if (interval <= 0)
                throw new ArgumentOutOfRangeException(nameof(interval));
            this.state = state;
            this.interval = new TimeSpan(interval * 10000);
            TimeSpan due = new TimeSpan(dueTime * 10000);
            if (period == -1)
            {
                next = DateTime.Now;
                timer = new Timer(Job, null, due, disabled);
            }
            else
            {
                periodTicks = period * 10000;
                next = DateTime.Now.AddTicks(periodTicks);
                timer = new Timer(Job, null, due, this.interval);
            }
        }

        /// <summary>
        /// Initializes a new instance of the TimerFix class, using 32-bit unsigned integers to measure time intervals.
        /// </summary>
        /// <param name="timerCallback">A delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before callback is invoked, in milliseconds. Specify Infinite to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of callback, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        /// <param name="interval">The time, in milliseconds, between checking whether the period is passed. Default value is 15.</param>
        [System.CLSCompliant(false)]
        public TimerFix(TimerCallback timerCallback, object state, uint dueTime, uint period, uint interval = 15)
        {
            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (interval == 0)
                throw new ArgumentOutOfRangeException(nameof(interval));
            long periodInLong = unchecked((int)period);
            if (periodInLong < 0 && periodInLong != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            this.state = state;
            this.interval = new TimeSpan((long)interval * 10000);
            TimeSpan due = new TimeSpan(dueTime * 10000);
            if (periodInLong == -1)
            {
                next = DateTime.Now;
                timer = new Timer(Job, null, due, disabled);
            }
            else
            {
                periodTicks = (long)period * 10000;
                next = DateTime.Now.AddTicks(periodTicks);
                timer = new Timer(Job, null, due, this.interval);
            }
        }

        /// <summary>
        /// Initializes a new instance of the TimerFix class, using TimeSpan values to measure time intervals.
        /// </summary>
        /// <param name="timerCallback">A delegate representing a method to be executed.</param>
        /// <param name="state">An object containing information to be used by the callback method, or null.</param>
        /// <param name="dueTime">The amount of time to delay before the callback parameter invokes its methods. Specify negative one (-1) milliseconds to prevent the timer from starting. Specify zero (0) to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the methods referenced by callback. Specify negative one (-1) milliseconds to disable periodic signaling.</param>
        /// <param name="interval">The time between checking whether the period is passed. Default value is 15 milliseconds.</param>
        public TimerFix(TimerCallback timerCallback, object state, TimeSpan dueTime, TimeSpan period, TimeSpan? interval)
        {
            TimeSpan intervalValue;
            if (interval.HasValue)
                intervalValue = interval.Value;
            else
                intervalValue = new TimeSpan(150000);

            this.timerCallback = timerCallback ?? throw new ArgumentNullException(nameof(timerCallback));
            if (period < TimeSpan.Zero && period.TotalMilliseconds != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            else if (period.TotalMilliseconds > 4294967294)
                throw new NotSupportedException(nameof(period) + " greater than 4294967294 milliseconds is not supported.");
            if (intervalValue <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(interval));
            this.state = state;
            this.interval = intervalValue;
            if (period.TotalMilliseconds == -1)
            {
                next = DateTime.Now;
                timer = new Timer(Job, null, dueTime, disabled);
            }
            else
            {
                periodTicks = period.Ticks;
                next = DateTime.Now.AddTicks(periodTicks);
                timer = new Timer(Job, null, dueTime, this.interval);
            }
        }

        void Job(object stateInfo)
        {
            bool lockResult = false;
            try
            {
                spinLock.Enter(ref lockResult);
                if (DateTime.Now >= next)
                {
                    timerCallback(state);

                    next = next.AddTicks(periodTicks);
                }
            }
            finally
            {
                spinLock.Exit();
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

            if (period < 0 && period != -1)
                throw new ArgumentOutOfRangeException(nameof(period));

            bool lockResult = false;
            try
            {
                spinLock.Enter(ref lockResult);
                if (period == -1)
                {
                    periodTicks = 0;
                    next = DateTime.Now;
                    timer.Change(dueTime, -1);
                }
                else
                {
                    periodTicks = period * 10000;
                    next = DateTime.Now.AddTicks(periodTicks);
                    timer = new Timer(Job, null, new TimeSpan(dueTime * 10000), interval);
                }
            }
            finally
            {
                spinLock.Exit();
            }
            return true;
        }

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 64-bit signed integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the Timer was constructed, in milliseconds. Specify Infinite to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        /// <returns>true if the timer was successfully updated; otherwise, false. Actually, only true will be returned.</returns>
        public bool Change(long dueTime, long period)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (period < 0 && period != -1)
                throw new ArgumentOutOfRangeException(nameof(period));

            bool lockResult = false;
            try
            {
                spinLock.Enter(ref lockResult);
                if (period == -1)
                {
                    periodTicks = 0;
                    next = DateTime.Now;
                    timer.Change(new TimeSpan(dueTime * 10000), disabled);
                }
                else
                {
                    periodTicks = period * 10000;
                    next = DateTime.Now.AddTicks(periodTicks);
                    timer = new Timer(Job, null, new TimeSpan(dueTime * 10000), interval);
                }
            }
            finally
            {
                spinLock.Exit();
            }
            return true;
        }

        /// <summary>
        /// Changes the start time and the interval between method invocations for a timer, using 32-bit unsigned integers to measure time intervals.
        /// </summary>
        /// <param name="dueTime">The amount of time to delay before the invoking the callback method specified when the Timer was constructed, in milliseconds. Specify Infinite to prevent the timer from restarting. Specify zero (0) to restart the timer immediately.</param>
        /// <param name="period">The time interval between invocations of the callback method specified when the Timer was constructed, in milliseconds. Specify Infinite to disable periodic signaling.</param>
        /// <returns>true if the timer was successfully updated; otherwise, false. Actually, only true will be returned.</returns>
        [System.CLSCompliant(false)]
        public bool Change(uint dueTime, uint period)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            long periodInLong = unchecked((int)period);
            if (periodInLong < 0 && periodInLong != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            bool lockResult = false;
            try
            {
                spinLock.Enter(ref lockResult);
                if (periodInLong == -1)
                {
                    periodTicks = 0;
                    next = DateTime.Now;
                    timer.Change(new TimeSpan(dueTime * 10000), disabled);
                }
                else
                {
                    periodTicks = periodInLong * 10000;
                    next = DateTime.Now.AddTicks(periodTicks);
                    timer = new Timer(Job, null, new TimeSpan(dueTime * 10000), interval);
                }
            }
            finally
            {
                spinLock.Exit();
            }
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

            if (period < TimeSpan.Zero && period.TotalMilliseconds != -1)
                throw new ArgumentOutOfRangeException(nameof(period));
            else if (period.TotalMilliseconds > 4294967294)
                throw new NotSupportedException(nameof(period) + " greater than 4294967294 milliseconds is not supported.");
            bool lockResult = false;
            try
            {
                spinLock.Enter(ref lockResult);
                if (period.TotalMilliseconds == -1)
                {
                    periodTicks = 0;
                    next = DateTime.Now;
                    timer.Change(dueTime, new TimeSpan(0, 0, 0, -1));
                }
                else
                {
                    periodTicks = period.Ticks;
                    next = DateTime.Now.Add(period);
                    timer = new Timer(Job, null, dueTime, interval);
                }
            }
            finally
            {
                spinLock.Exit();
            }
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
                    timer.Dispose();
                }
                timer = null;
                timerCallback = null;
                disposedValue = true;
            }
        }

#if !oldcode
        /// <summary>
        /// Releases all resources used by the current instance of TimerFix and signals when the timer has been disposed of.
        /// </summary>
        /// <param name="notifyObject">The WaitHandle to be signaled when the Timer has been disposed of.</param>
        /// <returns>true if the function succeeds; otherwise, false.</returns>
        public bool Dispose(WaitHandle notifyObject)
        {
            var result = timer.Dispose(notifyObject);
            Dispose(false);
            return result;
        }
#endif


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
