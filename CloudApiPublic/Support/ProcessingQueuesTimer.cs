using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudApiPublic.Model;
using CloudApiPublic.Static;

namespace CloudApiPublic.Support
{
    /// <summary>
    /// Class to handle queueing up processing changes on a configurable timer,
    /// must be externally locked on property TimerRunningLocker for all access
    /// </summary>
    public sealed class ProcessingQueuesTimer
    {
        /// <summary>
        /// Returns whether the current processing queue timer is running;
        /// Lock on this instance's TimerRunningLocker for getting this property
        /// </summary>
        public bool TimerRunning
        {
            get
            {
                return _timerRunning;
            }
        }
        private bool _timerRunning = false;
        /// <summary>
        /// Lock on this object anywhere that starts the timer or checks if it is running
        /// </summary>
        public readonly object TimerRunningLocker = new object();
        private Action OnTimeout;
        private int MillisecondTime;

        private ManualResetEvent SleepEvent = new ManualResetEvent(false);

        /// <summary>
        /// Creates and outputs a new ProcessingQueuesTimer which will execute the provided action whenever the timer is started and then runs out
        /// </summary>
        /// <param name="onTimeout">Action to run when timer runs out</param>
        /// <param name="millisecondTime">Length of timer whenever it is started</param>
        /// <param name="newTimer">Outputs the new ProcessingQueuesTimer that was created</param>
        /// <returns>Returns an error creating the ProcessingQueuesTimer, if any</returns>
        public static CLError CreateAndInitializeProcessingQueuesTimer(Action onTimeout, int millisecondTime, out ProcessingQueuesTimer newTimer)
        {
            try
            {
                newTimer = new ProcessingQueuesTimer(onTimeout, millisecondTime);
            }
            catch (Exception ex)
            {
                newTimer = Helpers.DefaultForType<ProcessingQueuesTimer>();
                return ex;
            }
            return null;
        }

        private ProcessingQueuesTimer(Action onTimeout, int millisecondTime)
        {
            if (onTimeout == null)
            {
                throw new NullReferenceException("onTimeout cannot be null");
            }
            this.OnTimeout = onTimeout;
            this.MillisecondTime = millisecondTime;
        }

        /// <summary>
        /// If the current timer is not running, it starts the timer,
        /// otherwise the timer continues running as it was before;
        /// must be externally locked on property TimerRunningLocker for all access
        /// </summary>
        public void StartTimerIfNotRunning()
        {
            if (!_timerRunning)
            {
                _timerRunning = true;
                (new Thread(() =>
                {
                    bool SleepEventNeedsReset = SleepEvent.WaitOne(this.MillisecondTime);
                    lock (TimerRunningLocker)
                    {
                        if (SleepEventNeedsReset)
                        {
                            SleepEvent.Reset();
                        }
                        _timerRunning = false;
                        OnTimeout();
                    }
                })).Start();
            }
        }

        /// <summary>
        /// If the current timer is not running, it just runs the action immediately;
        /// must be externally locked on property TimerRunningLocker for all access
        /// </summary>
        public void TriggerTimerCompletionImmediately()
        {
            if (_timerRunning)
            {
                SleepEvent.Set();
            }
            else
            {
                OnTimeout();
            }
        }
    }
}