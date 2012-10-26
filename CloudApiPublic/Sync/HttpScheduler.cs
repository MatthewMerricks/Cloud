//
// HttpScheduler.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudApiPublic.Interfaces;

namespace CloudApiPublic.Sync
{
    /// <summary>
    /// Extended TaskScheduler for Sync upload and download tasks to limit concurrency and handle exceptions;
    /// grab one instance or the over via static method GetSchedulerByDirection
    /// </summary>
    public sealed class HttpScheduler : TaskScheduler, IDisposable
    {
        #region private fields
        private const int _fromConcurrencyLevel = 6;// Limit of 6 simultaneous downloads
        private const int _toConcurrencyLevel = 6;// Limit of 6 simultaneous uploads

        // stores if this scheduler is for uploads or downloads; only one of each type exists
        private SyncDirection Direction;
        // the following two bools are used to tell if a scheduler has been previously disposed
        private static bool FromDisposed = false;
        private static bool ToDisposed = false;

        private static ISyncSettings SyncSettings
        {
            get
            {
                lock (SyncSettingsLocker)
                {
                    return _syncSettings;
                }
            }
        }
        private static ISyncSettings _syncSettings = null;
        private static readonly object SyncSettingsLocker = new object();
        #endregion

        // private constructor to ensure single instance instantiations,
        // used with type of scheduler (upload or download)
        private HttpScheduler(SyncDirection direction)
            : base()
        {
            this.Direction = direction;
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        ~HttpScheduler()
        {
            this.Dispose(false);
        }

        #region public instance accessor
        /// <summary>
        /// Instance accessor for this HttpScheduler, use the return instance as a parameter for [Task].Start
        /// </summary>
        /// <param name="direction">Use From for downloads and To for uploads</param>
        /// <returns>Returns the appropriate instance of HttpScheduler</returns>
        public static HttpScheduler GetSchedulerByDirection(SyncDirection direction, ISyncSettings syncSettings)
        {
            // switch on input direction so the appropriate output is returned
            switch (direction)
            {
                case SyncDirection.From:
                    // direction for downloads
                    lock (instanceFromLocker)
                    {
                        // if download scheduler is null, create it and attach its exception handler to the base type
                        if (_instanceFrom == null)
                        {
                            lock (SyncSettingsLocker)
                            {
                                if (HttpScheduler._syncSettings == null)
                                {
                                    if (syncSettings == null)
                                    {
                                        throw new NullReferenceException("syncSettings cannot be null");
                                    }
                                    HttpScheduler._syncSettings = syncSettings;
                                }
                            }

                            _instanceFrom = new HttpScheduler(SyncDirection.From);
                            lock (GCOverrideLocker)
                            {
                                if (!GCOverrideInitialized)
                                {
                                    try
                                    {
                                        IsGCOverridden = OverrideGC();
                                    }
                                    finally
                                    {
                                        GCOverrideInitialized = true;
                                    }
                                }
                            }
                            TaskScheduler.UnobservedTaskException += _instanceFrom.TaskScheduler_UnobservedTaskException;
                        }
                        // return download scheduler
                        return _instanceFrom;
                    }
                case SyncDirection.To:
                    // direction for uploads
                    lock (instanceToLocker)
                    {
                        // if upload scheduler is null, create it and attach its exception handler to the base type
                        if (_instanceTo == null)
                        {
                            lock (SyncSettingsLocker)
                            {
                                if (HttpScheduler._syncSettings == null)
                                {
                                    if (syncSettings == null)
                                    {
                                        throw new NullReferenceException("syncSettings cannot be null");
                                    }
                                    HttpScheduler._syncSettings = syncSettings;
                                }
                            }

                            _instanceTo = new HttpScheduler(SyncDirection.To);
                            lock (GCOverrideLocker)
                            {
                                try
                                {
                                    IsGCOverridden = OverrideGC();
                                }
                                finally
                                {
                                    GCOverrideInitialized = true;
                                }
                            }
                            TaskScheduler.UnobservedTaskException += _instanceTo.TaskScheduler_UnobservedTaskException;
                        }
                        // return upload scheduler
                        return _instanceTo;
                    }
                default:
                    // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                    throw new NotSupportedException("Unknown SyncDirection: " + direction.ToString());
            }
        }
        private static HttpScheduler _instanceFrom = null;
        private static HttpScheduler _instanceTo = null;
        private static readonly object instanceFromLocker = new object();
        private static readonly object instanceToLocker = new object();
        #endregion

        // garbage collection is given a forced minimum interval so that Tasks with exceptions will be handled on a regular basis
        #region garbage collector override
        private static bool GCOverrideInitialized = false;
        public static bool IsGCOverridden { get; private set; }
        private static readonly object GCOverrideLocker = new object();
        private static bool OverrideGC()
        {
            try
            {
                lock (GCOverrideLocker)
                {
                    if (!GCOverrideInitialized
                        && (_instanceFrom != null || _instanceTo != null))
                    {
                        bool instanceFound;
                        lock (instanceFromLocker)
                        {
                            instanceFound = _instanceFrom != null;
                        }
                        if (!instanceFound)
                        {
                            lock (instanceToLocker)
                            {
                                instanceFound = _instanceTo != null;
                            }
                        }

                        (new Thread(() =>
                        {
                            // conditions for this while loop should cause the thread to keep running so long as both currently instantiated HttpSchedulers are not disposed;
                            // in other words, if at least one has been instantiated and all that have been instantiated have been disposed
                            Func<bool> continueGarbageCollecting = () =>
                            {
                                lock (instanceFromLocker)
                                {
                                    lock (instanceToLocker)
                                    {
                                        return !(_instanceFrom == null && _instanceTo == null)
                                            && ((!HttpScheduler.FromDisposed && !HttpScheduler.ToDisposed)
                                                || (HttpScheduler.FromDisposed && _instanceTo != null && !HttpScheduler.ToDisposed)
                                                || (_instanceFrom != null && !HttpScheduler.FromDisposed && HttpScheduler.ToDisposed));
                                    }
                                }
                            };
                            // if condition is met for continuing to override the minimum garbage collection interval
                            while (continueGarbageCollecting())
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();

                                int garbageCollectionRoundedSeconds = Convert.ToInt32(Math.Floor(GarbageCollectionMinimumIntervalSeconds));
                                double remainderSeconds = GarbageCollectionMinimumIntervalSeconds - (double)garbageCollectionRoundedSeconds;

                                if (remainderSeconds > 0)
                                {
                                    for (int sleepCounter = 0; sleepCounter < garbageCollectionRoundedSeconds; sleepCounter++)
                                    {
                                        Thread.Sleep(1000);// sleep for one second for this second iteration
                                        if (!continueGarbageCollecting())
                                        {
                                            return;
                                        }
                                    }
                                    Thread.Sleep((int)(remainderSeconds * 1000));
                                }
                                else
                                {
                                    for (int sleepCounter = 0; sleepCounter < garbageCollectionRoundedSeconds - 1; sleepCounter++)
                                    {
                                        Thread.Sleep(1000);// sleep for one second for this second iteration
                                        if (!continueGarbageCollecting())
                                        {
                                            return;
                                        }
                                    }
                                    Thread.Sleep(1000);// sleep for one second for this second iteration
                                }
                            }
                        })).Start();

                        GCOverrideInitialized = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ((CLError)ex).LogErrors(SyncSettings.ErrorLogLocation, SyncSettings.LogErrors);
                return false;
            }
            return true;
        }

        // Once HttpScheduler has been accessed at least once and hasn't been disposed,
        // the garbage collector will be forced at this minimum seconds interval:
        private const double GarbageCollectionMinimumIntervalSeconds = 10d;
        #endregion

        #region IDisposable member
        // Standard IDisposable implementation based on MSDN System.IDisposable
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        /// <summary>
        /// Finds the HttpSchedulers that exist and are not yet disposed and disposes them
        /// </summary>
        public static void DisposeBothSchedulers()
        {
            lock (instanceFromLocker)
            {
                if (_instanceFrom != null
                    && !HttpScheduler.FromDisposed)
                {
                    ((IDisposable)_instanceFrom).Dispose();
                }
            }
            lock (instanceToLocker)
            {
                if (_instanceTo != null
                    && !HttpScheduler.ToDisposed)
                {
                    ((IDisposable)_instanceTo).Dispose();
                }
            }
        }
        // Standard IDisposable implementation based on MSDN System.IDisposable
        private void Dispose(bool disposing)
        {
            // define bool for storing if the current direction's HttpScheduler is disposed,
            // defaulting to false until it is found to be disposed
            bool currentDirectionIsDisposed = false;

            // if the direction is From then pull Disposed from the FromDisposed
            if (this.Direction == SyncDirection.From)
            {
                lock (instanceFromLocker)
                {
                    if (FromDisposed)
                    {
                        currentDirectionIsDisposed = true;
                    }
                }
            }
            // else if the direction is To then pull Disposed from the ToDisposed
            else
            {
                lock (instanceToLocker)
                {
                    if (ToDisposed)
                    {
                        currentDirectionIsDisposed = true;
                    }
                }
            }

            // if the current direction's HttpScheduler has not been disposed,
            // process disposal logic
            if (!currentDirectionIsDisposed)
            {
                // if the current HttpScheduler's direction is From,
                // then set From as disposed
                if (this.Direction == SyncDirection.From)
                {
                    lock (instanceFromLocker)
                    {
                        FromDisposed = true;
                    }
                }
                // else if the current HttpScheduler's direction is To,
                // then set To as disposed
                else
                {
                    lock (instanceToLocker)
                    {
                        ToDisposed = true;
                    }
                }

                // The following section is commented out since there are no inner managed objects for this class yet
                //// Run dispose on inner managed objects based on disposing condition
                //if (disposing)
                //{
                //    // Dispose inner managed objects here
                //}

                // Dispose local unmanaged resources last
            }
        }

        // This handler will fire so long as the [Task].Exception was not retrieved and [Task].Wait was not fired;
        // It will observe the error to prevent application crash iff one of the exceptions was an ExecutableException
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            // if the current TaskScheduler sender is the current one,
            // then have this one log (and possibly observe) the exception

            Task castSender = sender as Task;

            // attempt to pull out "ExecutingTaskScheduler" private property value from sender Task
            object executingScheduler;
            if (castSender != null)
            {
                executingScheduler = ExecutingTaskSchedulerInfo.GetValue(castSender, null);
            }
            else
            {
                executingScheduler = null;
            }
            
            // if the sender Task's executing TaskScheduler was found and it matches the current HttpScheduler,
            // then the exception can be processed here
            if (this == executingScheduler)
            {
                LogException(this.Direction, e.Exception, e);
            }
        }
        // Reflected private property "ExecutingTaskScheduler" from Task which grabs a Task instance's executing TaskScheduler for comparison
        private static readonly PropertyInfo ExecutingTaskSchedulerInfo = (PropertyInfo)typeof(Task).GetMember("ExecutingTaskScheduler", BindingFlags.NonPublic | BindingFlags.Instance)[0];

        // Logs errors and prevents application crash for a non-null optional param event args iff one of the exceptions was an ExecutableException
        private static void LogException(SyncDirection direction, AggregateException ex, UnobservedTaskExceptionEventArgs e = null)
        {
            // the base exception of the CLError should be the base exception of the aggregated input;
            // keep in mind this appears to be a deep copy of the original exception since an object reference comparison returns false against all
            // exceptions in the Flatten().InnerExceptions below
            CLError aggregatedError = ex.GetBaseException();
            // append additional description to the 
            aggregatedError.errorDescription = (direction == SyncDirection.From ? "Download" : "Upload") + " HttpScheduler logged aggregate base: " + aggregatedError.errorDescription;

            // define bool to force logging exceptions regardless of Settings for the severe case of displaying a MessageBox to the user,
            // which only happens if the exception was not properly wrapped in a ExecutableException
            bool overrideLoggingOnMessageBox = false;

            // loop through all exceptions in the aggregate
            foreach (Exception currentError in ex.Flatten().InnerExceptions)
            {
                // add the exception to the CLError
                aggregatedError += currentError;

                // try the cast to IExecutableException so it can be fired
                IExecutableException castError = currentError as IExecutableException;
                if (castError != null)
                {
                    // Fires any pending exception handling 
                    aggregatedError += castError.ExecuteException(ex);

                    if (e != null
                        && !e.Observed)
                    {
                        // only observe the exception if it was executable since that means it was explicitly handled
                        e.SetObserved();
                    }
                }
                // otherwise the invalid cast needs to be displayed (severe case) and logged (overrides Settings)
                else
                {
                    // All exceptions should be wrapped as ExecutableExceptions. This message should make it obvious that an exception still needs to be wrapped.
                    // -David
                    MessageBox.Show("An error occurred while processing file " + (direction == SyncDirection.From ? "download" : "upload") + "." + Environment.NewLine +
                        "All exceptions should be wrapped as type ExecutableException so this message does not appear!" + Environment.NewLine +
                        "Exception message: " + currentError.Message);

                    overrideLoggingOnMessageBox = true;
                }
            }
            // performs the actual logging of errors, forces logging even if the setting is turned off in the severe case where a message box had to appear
            aggregatedError.LogErrors(SyncSettings.ErrorLogLocation, overrideLoggingOnMessageBox ? true : SyncSettings.LogErrors);
        }

        /// <summary>
        /// Returns the number of simultaneous tasks which can process for the current type of HttpScheduler
        /// </summary>
        public override int MaximumConcurrencyLevel
        {
            get
            {
                return this.Direction == SyncDirection.From
                    ? _fromConcurrencyLevel
                    : _toConcurrencyLevel;
            }
        }

        // define the storage queue for tasks to process
        private readonly Queue<Task> TaskQueue = new Queue<Task>();
        // define the locker for access to TaskQueue
        private readonly object TaskQueueLocker = new object();
        // define the current number of simultaneously processing tasks, defaulting to zero before the first Tasks queue
        private int runningTaskCount = 0;
        private int inlineExecutingCount = 0;

        // returns tasks which have yet to start processing from TaskQueue
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            Task[] toReturn;
            lock (TaskQueueLocker)
            {
                toReturn = new Task[TaskQueue.Count];
                TaskQueue.CopyTo(toReturn, 0);
            }
            return toReturn;
        }

        // runs a task on a new thread if too few of simultaneous tasks are running or queues it to run later otherwise
        protected override void QueueTask(Task task)
        {
            lock (TaskQueueLocker)
            {
                switch (Direction)
                {
                    case SyncDirection.From:
                    // direction for downloads
                        MessageEvents.SetDownloadingCount(this, (uint)(TaskQueue.Count + runningTaskCount + inlineExecutingCount + 1));
                        break;

                    case SyncDirection.To:
                    // direction for uploads
                        MessageEvents.SetUploadingCount(this, (uint)(TaskQueue.Count + runningTaskCount + inlineExecutingCount + 1));
                        break;

                    default:
                        // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                        throw new NotSupportedException("Unknown SyncDirection: " + Direction.ToString());
                }

                if (runningTaskCount < MaximumConcurrencyLevel)
                {
                    runningTaskCount++;
                    ThreadPool.UnsafeQueueUserWorkItem(TaskProcessor, new KeyValuePair<HttpScheduler, Task>(this, task));
                }
                else
                {
                    TaskQueue.Enqueue(task);
                }
            }
        }

        // delegate fired by the new task-running thread which can immediately run a provided task
        // and will later try to pull more tasks out of TaskQueue till it's empty
        private static void TaskProcessor(object state)
        {
            Nullable<KeyValuePair<HttpScheduler, Task>> castState = state as Nullable<KeyValuePair<HttpScheduler, Task>>;
            if (castState == null)
            {
                // Must make this error very clear because we cannot recover from being unable to run tasks
                // -David
                MessageBox.Show("TaskProcessor requires a HttpScheduler user state to run");
            }
            else
            {
                KeyValuePair<HttpScheduler, Task> nonNullState = (KeyValuePair<HttpScheduler, Task>)castState;

                Action<HttpScheduler> setCount = scheduler =>
                    {
                        uint taskCount;
                        lock (scheduler.TaskQueueLocker)
                        {
                            taskCount = (uint)(scheduler.TaskQueue.Count + scheduler.runningTaskCount + scheduler.inlineExecutingCount - 1);
                        }

                        switch (scheduler.Direction)
                        {
                            case SyncDirection.From:
                            // direction for downloads
                                MessageEvents.SetDownloadingCount(scheduler, taskCount);
                                break;

                            case SyncDirection.To:
                            // direction for uploads
                                MessageEvents.SetUploadingCount(scheduler, taskCount);
                                break;

                            default:
                                // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                                throw new NotSupportedException("Unknown SyncDirection: " + scheduler.Direction.ToString());
                        }
                    };

                // if the provided userstate contains a Task to start with,
                // then try to execute that task immediately
                if (nonNullState.Value != null)
                {
                    nonNullState.Key.TryExecuteTask(nonNullState.Value);

                    setCount(nonNullState.Key);
                }

                // define function to check if a given direction's HttpScheduler is disposed
                Func<SyncDirection, bool> checkDisposed = direction =>
                {
                    // if the direction is From then pull Disposed from the FromDisposed
                    if (direction == SyncDirection.From)
                    {
                        lock (instanceFromLocker)
                        {
                            return FromDisposed;
                        }
                    }
                    // else if the direction is To then pull Disposed from the ToDisposed
                    else
                    {
                        lock (instanceToLocker)
                        {
                            return ToDisposed;
                        }
                    }
                };

                // loop until the current direction's HttpScheduler is disposed
                while (!checkDisposed(nonNullState.Key.Direction))
                {
                    // declare the Task that may be run
                    Task toRun;
                    lock (nonNullState.Key.TaskQueueLocker)
                    {
                        // if there are no Tasks in the queue to run,
                        // then decrement the count of concurrently running tasks and break out to stop executing
                        if (nonNullState.Key.TaskQueue.Count == 0)
                        {
                            nonNullState.Key.runningTaskCount--;
                            break;
                        }

                        // if we have not broken out, then a Task exists to dequeue and run
                        toRun = nonNullState.Key.TaskQueue.Dequeue();
                    }

                    // run the dequeued Task
                    nonNullState.Key.TryExecuteTask(toRun);

                    setCount(nonNullState.Key);
                }
            }
        }

        // Removes a single task from the queue without processing;
        // this is done very inefficiently since Queues are cannot have an item removed as easily as a doubly-linked list,
        // but this is acceptable since this method should not run often
        protected override bool TryDequeue(Task task)
        {
            lock (TaskQueueLocker)
            {
                Task[] existingTasks = new Task[TaskQueue.Count];
                int existingLocation = -1;
                for (int existingTaskIndex = 0; existingTaskIndex < TaskQueue.Count; existingTaskIndex++)
                {
                    if ((existingTasks[existingTaskIndex] = TaskQueue.Dequeue()) == task)
                    {
                        existingLocation = existingTaskIndex;
                    }
                }
                for (int existingTaskIndex = 0; existingTaskIndex < existingTasks.Length; existingTaskIndex++)
                {
                    if (existingTaskIndex != existingLocation)
                    {
                        TaskQueue.Enqueue(existingTasks[existingTaskIndex]);
                    }
                }
                return existingLocation != -1;
            }
        }

        // Runs a task synchronously, removing it from the queue as necessary
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            // run under lock of TaskQueueLocker
            Action<HttpScheduler> setCount = scheduler =>
            {
                uint taskCount = (uint)(scheduler.TaskQueue.Count + scheduler.runningTaskCount + scheduler.inlineExecutingCount);

                switch (scheduler.Direction)
                {
                    case SyncDirection.From:
                    // direction for downloads
                        MessageEvents.SetDownloadingCount(scheduler, taskCount);
                        break;

                    case SyncDirection.To:
                    // direction for uploads
                        MessageEvents.SetUploadingCount(scheduler, taskCount);
                        break;

                    default:
                        // if a new SyncDirection was added, this class needs to be updated to work with it, until then, throw this exception
                        throw new NotSupportedException("Unknown SyncDirection: " + scheduler.Direction.ToString());
                }
            };

            if (taskWasPreviouslyQueued)
            {
                TryDequeue(task);
            }

            lock (TaskQueueLocker)
            {
                inlineExecutingCount++;
                setCount(this);
            }

            bool executed = base.TryExecuteTask(task);

            lock (TaskQueueLocker)
            {
                inlineExecutingCount--;
                setCount(this);
            }

            // log the exception synchronously since the task fired synchronously
            if (executed
                && task.IsFaulted)
            {
                LogException(this.Direction, task.Exception);
            }
            return executed;
        }
    }
}