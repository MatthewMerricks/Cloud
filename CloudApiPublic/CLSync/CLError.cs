//
//  CLError.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace Cloud
{
    /// <summary>
    /// Class that represents an error, and supports logging of the error.  The error may contain one or more exceptions.
    /// </summary>
    public sealed class CLError
    {
        private List<Stream> streams = null;

        /// <summary>
        /// Returns the collection of exceptions causing the error
        /// </summary>
        public ReadOnlyCollection<CLException> Exceptions
        {
            get
            {
                return new ReadOnlyCollection<CLException>(_exceptions.Count == 0 ? new List<CLException>(Helpers.EnumerateSingleItem(PrimaryException)) : _exceptions);
            }
        }
        private readonly List<CLException> _exceptions;

        internal bool IsExceptionsBackingFieldEmpty
        {
            get
            {
                return _exceptions.Count == 0;
            }
        }

        /// <summary>
        /// Returns the primary exception causing the error
        /// </summary>
        public CLException PrimaryException
        {
            get
            {
                if (_exceptions.Count == 0)
                {
                    try
                    {
                        throw new CLException(
                            CLExceptionCode.General_Miscellaneous,
                            (streams == null
                                ? "CLExceptions does not contain a PrimaryException"
                                : "FileStream added first instead of exception"));
                    }
                    catch (CLException ex)
                    {
                        return ex;
                    }
                }
                else
                {
                    return _exceptions[0];
                }
            }
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLError(params CLException[] exceptions)
        {
            _exceptions = new List<CLException>(exceptions);
        }

        /// <summary>
        /// Set the last state of the SyncEngine running thread before the error will be logged
        /// </summary>
        internal string SyncEngineRunStatus
        {
            set
            {
                _syncEngineRunStatus = value;
            }
        }
        private string _syncEngineRunStatus = null;

        internal void SetHttpSchedulerLogged(SyncDirection direction)
        {
            httpSchedulerLogged = direction;
        }
        private Nullable<SyncDirection> httpSchedulerLogged = null;

        /// <summary>
        /// Implicitly converts from an exception. Adds the exception to this error including its message, domain, and code.
        /// </summary>
        /// <param name="ex">Implictly converted exception</param>
        /// <returns>Returns CLError resulting from implicit conversion</returns>
        public static implicit operator CLError(Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            CLException castEx = ex as CLException;
            if (castEx == null)
            {
                castEx = new CLException(
                    CLExceptionCode.General_Miscellaneous,
                    ex.Message,
                    ex);
            }

            return new CLError(castEx);
        }

        // Added so we can append FileStream objects which can later be disposed upon error handling
        // -David
        internal void AddFileStream(Stream fStream)
        {
            if (fStream != null)
            {
                if (streams == null)
                {
                    streams = new List<Stream>(Helpers.EnumerateSingleItem(fStream));
                }
                else
                {
                    streams.Add(fStream);
                }
            }
        }

        // Added so we can append FileStream objects which can later be disposed upon error handling
        // -David
        /// <summary>
        /// Do not use this implicit operator, other Stream-related operations for CLError are not exposed
        /// </summary>
        public static CLError operator +(CLError err, Stream fStream)
        {
            if (fStream == null)
            {
                return err;
            }
            if (err == null)
            {
                err = new CLError();
            }

            err.AddFileStream(fStream);
            return err;
        }

        /// <summary>
        /// Takes all the FileStream instances out of this error and returns them for disposal
        /// </summary>
        /// <returns>Returns dequeued FileStream objects</returns>
        internal IEnumerable<Stream> DequeueStreams()
        {
            return Helpers.RemoveAllFromList(streams);
        }

        /// <summary>
        /// Aggregation operator to add a first or additional exceptions to a CLError
        /// </summary>
        /// <param name="err">Existing error, or null to create a new one via implicit conversion</param>
        /// <param name="ex">Exception to add or start with</param>
        /// <returns></returns>
        public static CLError operator +(CLError err, Exception ex)
        {
            if (err == null)
            {
                return ex;
            }

            err.AddException(ex);
            return err;
        }

        /// <summary>
        /// Appends an exception to this error
        /// </summary>
        /// <param name="ex">To append</param>
        /// <param name="replacePrimaryError">(optional) Whether the message of this error should be set from the currently appended exception</param>
        internal void AddException(Exception ex, bool replacePrimaryError = false)
        {
            if (ex != null)
            {
                CLException castEx = ex as CLException;
                if (castEx == null)
                {
                    castEx = new CLException(
                        CLExceptionCode.General_Miscellaneous,
                        ex.Message,
                        ex);
                }

                if (replacePrimaryError)
                {
                    _exceptions.Insert(/* index */ 0, castEx);
                }
                else
                {
                    _exceptions.Add(castEx);
                }
            }
        }

        /// <summary>
        /// Logs error information from this CLError to the specified location if loggingEnabled parameter is passed as true
        /// </summary>
        /// <param name="logLocation">Base location for log files before date and extension are appended</param>
        /// <param name="loggingEnabled">Determines whether logging will actually occur</param>
        [MethodImpl(MethodImplOptions.Synchronized)] // synchronized so multiple logs don't try writing to the files simultaneously
        public void Log(string logLocation, bool loggingEnabled)
        {
            // skip logging if it is either disabled or the location of the log is invalid
            if (loggingEnabled
                && !string.IsNullOrWhiteSpace(logLocation))
            {
                try
                {
                    // Determine the full path of the trace file to use and manage the number of old trace files.
                    string logFilePath = Helpers.CheckLogFileExistance(TraceLocation: logLocation, SyncboxId: null, UserDeviceId: null, TraceCategory: "CloudError",
                        FileExtensionWithoutPeriod: Resources.IconOverlayLog, OnNewTraceFile: null, OnPreviousCompletion: null);

                    // Write the info to the trace file
                    try
                    {
                        // build FileStream with path built from base log name and current date
                        using (FileStream logStream = new FileStream(logFilePath,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.Read))
                        {
                            // grab a writer for appending to the log in a auto-disposing context
                            using (StreamWriter logWriter = new StreamWriter(logStream))
                            {
                                logWriter.WriteLine("Time: " + DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK")); // ISO 8601 (dropped seconds)
                                logWriter.WriteLine("ProcessId: " + System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                                logWriter.WriteLine("ThreadId: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());

                                // for each custom error status,
                                // if they exist then write them first to the log
                                #region custom error key statuses
                                if (!string.IsNullOrEmpty(this._syncEngineRunStatus))
                                {
                                    logWriter.WriteLine(this._syncEngineRunStatus);
                                }

                                if (this.httpSchedulerLogged != null)
                                {
                                    logWriter.WriteLine((((SyncDirection)this.httpSchedulerLogged) == SyncDirection.From ? "Download" : "Upload") +
                                        " HttpScheduler logged aggregate base:");
                                }
                                #endregion

                                // create a queue for exceptions which need to be logged with stacktrace and recursive inner exception messages
                                Queue<Exception> loggableExceptions = new Queue<Exception>();

                                foreach (CLException currentException in _exceptions)
                                {
                                    if (currentException != null)
                                    {
                                        loggableExceptions.Enqueue(currentException);
                                    }
                                    
                                    // loop while there are still exceptions to log in the queue
                                    while (loggableExceptions.Count > 0)
                                    {
                                        // dequeue the current exception to log
                                        Exception dequeuedException = loggableExceptions.Dequeue();

                                        // define a string for storing the StackTrace, defaulting to null
                                        string stack = null;
                                        // I don't know if it's dangerous to pull out the StackTrace, so I wrap it safely
                                        try
                                        {
                                            stack = (new System.Diagnostics.StackTrace(dequeuedException, fNeedFileInfo: true)).ToString();
                                        }
                                        catch
                                        {
                                            try
                                            {
                                                stack = dequeuedException.StackTrace;
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        // keep track of how many inner exceptions have recursed to increase tab amount
                                        int tabCounter = 1;

                                        // traverse through inner exceptions, each time with an extra tab appended
                                        while (dequeuedException != null)
                                        {
                                            AggregateException dequeuedAsAggregate = dequeuedException as AggregateException;
                                            CLException dequeuedAsCLException;
                                            if (dequeuedAsAggregate == null)
                                            {
                                                dequeuedAsCLException = null;
                                            }
                                            else
                                            {
                                                dequeuedAsCLException = dequeuedAsAggregate as CLException;

                                                if (dequeuedAsCLException != null)
                                                {
                                                    logWriter.WriteLine(
                                                        Helpers.MakeTabs(tabCounter) +
                                                            string.Format(Resources.LogCloudExceptionType,
                                                                dequeuedAsCLException.GetType().Name));
                                                }
                                            }

                                            // write the current exception message to the log after the tabCounter worth of tabs
                                            logWriter.WriteLine(
                                                Helpers.MakeTabs(tabCounter++) + // increment the tab count so the next message will be more indented
                                                    dequeuedException.Message);

                                            if (dequeuedAsCLException != null)
                                            {
                                                CLHttpException dequeuedAsHttpException = dequeuedAsCLException as CLHttpException;

                                                if (dequeuedAsHttpException != null)
                                                {
                                                    if (dequeuedAsHttpException.HttpStatus != null)
                                                    {
                                                        logWriter.WriteLine(
                                                            Helpers.MakeTabs(tabCounter) +
                                                                string.Format(Resources.LogHttpStatus,
                                                                    (int)((HttpStatusCode)dequeuedAsHttpException.HttpStatus)));
                                                    }

                                                    if (!string.IsNullOrEmpty(dequeuedAsHttpException.HttpResponse))
                                                    {
                                                        logWriter.WriteLine(
                                                            Helpers.MakeTabs(tabCounter) +
                                                                string.Format(Resources.LogHttpResponse,
                                                                    Environment.NewLine,
                                                                    dequeuedAsHttpException.HttpResponse));
                                                    }
                                                }
                                            }

                                            // if the current exception is an aggregate, then enqueue its inner exceptions to relog (for viewing their stacktrace)
                                            if (dequeuedAsAggregate != null)
                                            {
                                                foreach (Exception aggregatedException in
                                                    dequeuedAsAggregate.InnerExceptions)
                                                {
                                                    loggableExceptions.Enqueue(aggregatedException);
                                                }
                                            }

                                            // prepare for next inner exception traversal
                                            dequeuedException = dequeuedException.InnerException;
                                        }

                                        // if a StackTrace was found,
                                        // then write it to the log
                                        if (!string.IsNullOrWhiteSpace(stack))
                                        {
                                            // write the StackTrace to the log with 1 tab
                                            logWriter.WriteLine(
                                                Helpers.MakeTabs() +
                                                    string.Format(Resources.LogStacktrace,
                                                        Environment.NewLine,
                                                        stack));
                                        }
                                    }
                                }

                                // end log with one extra line to seperate from next error entries
                                logWriter.WriteLine();
                                logWriter.Flush();
                                logStream.Flush();
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                catch
                {
                }
            }
        }
    }
}