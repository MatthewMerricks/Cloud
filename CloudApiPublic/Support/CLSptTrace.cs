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
using System.Threading;

namespace CloudApiPublic.Support
{
#region "Trace Singleton"

    /// <summary>
    /// A Logging class implementing the Singleton pattern
    /// </summary>
    public class CLTrace
    {
        private static CLTrace _instance;
        private static object _instanceLocker = new object();
        private static int _maxPriority = 10;  // set this to the highest priority to log
        private static string _logDir = @"c:\Trash\Trace";
        private static string _logFile = string.Format(@"\Trace-{0:yyyy-MM-dd}.txt", DateTime.Now);

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
                        Directory.CreateDirectory(_logDir);
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
            if (priority <= _maxPriority)
            {
                // Lock while writing to prevent contention for the log file
                lock (_instance)
                {
                    // Format the string
                    string message = string.Format(format, args);

                    // Create the entry
                    LogMessage logEntry = new LogMessage(priority, message);

                    // Log to the VS output window, or to DbgView
                    Debug.WriteLine(string.Format("{0}_{1}_{2}_{3}", logEntry.LogTime, logEntry.ProcessId.ToString("x"), logEntry.ThreadId.ToString("x"), logEntry.Message));

                    string logPath = _logDir + _logFile;

                    // This could be optimised to prevent opening and closing the file for each write
                    using (FileStream fs = File.Open(logPath, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        using (StreamWriter log = new StreamWriter(fs))
                        {
                            log.WriteLine(string.Format("{0}_{1}_{2}_{3}", logEntry.LogTime, logEntry.ProcessId.ToString("x"), logEntry.ThreadId.ToString("x"), logEntry.Message));
                        }
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
        public int Priority { get; set; }
        public int ProcessId { get; set; }
        public int ThreadId { get; set; }

        public LogMessage(int priority, string message)
        {
            Message = message;
            DateTime currentTime = DateTime.Now;
            LogDate = currentTime.ToString("yyyy-MM-dd");
            LogTime = currentTime.ToString("hh:mm:ss.fff tt");
            Priority = priority;
            ProcessId = Process.GetCurrentProcess().Id;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
        }
    }

    #endregion
}
