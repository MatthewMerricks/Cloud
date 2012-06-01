//
//  CLTrace.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace CloudApiPublic.Support
{
#region "Trace Singleton"

    /// <summary>
    /// A Logging class implementing the Singleton pattern and an internal Queue to be flushed perdiodically
    /// </summary>
    public class CLTrace
    {
        private static CLTrace _instance;
        private static object _instanceLocker = new object();
        private static Queue<LogMessage> logQueue;
        private static string logDir = @"c:\Trash\Trace";
        private static string logFile = string.Format(@"\Trace-{0:yyyy-MM-dd_hh-mm-ss-tt}.txt", DateTime.Now);

        private static int maxLogAge = 1;     // seconds to flush memory to disk
        private static int maxPriority = 10;  // set this to the highest priority to log
        private static int queueSize = 100;   // messages in memory
        private static DateTime LastFlushed = DateTime.Now;

        /// <summary>
        /// Private constructor to prevent instance creation
        /// </summary>
        private CLTrace() { }

        /// <summary>
        /// An LogWriter instance that exposes a single instance
        /// </summary>
        public static CLTrace Instance
        {
            get
            {
                lock (_instanceLocker)
                {
                    // If the instance is null then create one and init the Queue
                    if (_instance == null)
                    {
                        _instance = new CLTrace();
                        logQueue = new Queue<LogMessage>();
                        Directory.CreateDirectory(logDir);
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="priority">The priority of the message.  0 is highest.</param>
        /// <param name="message">The message to write to the log</param>
        public void writeToLog(int priority, string format, params object[] args)
        {
            // Only write high priority messages
            if (priority <= maxPriority)
            {
                // Lock the queue while writing to prevent contention for the log file
                lock (logQueue)
                {
                    // Format the string
                    string message = string.Format(format, args);

                    // Create the entry
                    LogMessage logEntry = new LogMessage(priority, message);

                    // Log to the VS output window, or to DbgView
                    Debug.WriteLine(string.Format("{0}\t{1}", logEntry.LogTime, logEntry.Message));

                    // Enqueue the entry to a memory queue
                    logQueue.Enqueue(logEntry);

                    // If we have reached the Queue Size then flush the Queue
                    if (logQueue.Count >= queueSize || doPeriodicFlush())
                    {
                        flushLog();
                    }
                }
            }
        }


        /// <summary>
        /// Flush the queue to disk.
        /// </summary>
        public void flush()
        {
            flushLog();
        }

        private bool doPeriodicFlush()
        {
            TimeSpan logAge = DateTime.Now - LastFlushed;
            if (logAge.TotalSeconds >= maxLogAge)
            {
                LastFlushed = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Flushes the Queue to the physical log file
        /// </summary>
        private void flushLog()
        {
            while (logQueue.Count > 0)
            {
                LogMessage entry = logQueue.Dequeue();
                string logPath = logDir + logFile;

                // This could be optimised to prevent opening and closing the file for each write
                using (FileStream fs = File.Open(logPath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter log = new StreamWriter(fs))
                    {
                        log.WriteLine(string.Format("{0}\t{1}", entry.LogTime, entry.Message));
                    }
                }
            }
        }
    }

    /// <summary>
    /// A Log class to store the message and the Date and Time the log entry was created
    /// </summary>
    public class LogMessage
    {
        public string Message { get; set; }
        public string LogTime { get; set; }
        public string LogDate { get; set; }
        public int Priority {get; set; }

        public LogMessage(int priority, string message)
        {
            Message = message;
            LogDate = DateTime.Now.ToString("yyyy-MM-dd");
            LogTime = DateTime.Now.ToString("hh:mm:ss.fff tt");
            Priority = priority;
        }
    }

    #endregion
}
