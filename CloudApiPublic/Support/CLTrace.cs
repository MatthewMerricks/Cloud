﻿//
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
using Cloud.Static;
using System.Text.RegularExpressions;

namespace Cloud.Support
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
        private static string _traceCategory = null;
        private static string _fileExtensionWithoutPeriod = null;

        /// <summary>
        /// The full path of the folder where trace files will be placed.
        /// </summary>
        public string TraceLocation
        {
            get { return _traceLocation; }
            set { _traceLocation = value; }
        }
        private static string _traceLocation = null;

        /// <summary>
        /// Specifies whether a separate file with only the traced errors will be created.
        /// </summary>
        public bool LogErrors
        {
            get { return _logErrors; }
            set { _logErrors = value; }
        }
        private static bool _logErrors = false;      // trace errors

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
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Call this function before any other CLTrace call.
        /// </summary>
        /// <param name="TraceLocation">The full path of the trace directory.</param>
        /// <param name="TraceCategory">The name of the trace category.</param>
        /// <param name="FileExtensionWithoutPeriod">e.g.: "log".</param>
        /// <param name="TraceLevel">0: No trace.  Enter 1 for most important traces.  Higher numbers for greater detail.</param>
        /// <param name="LogErrors">The Settings LogErrors setting.</param>
        /// <param name="willForceReset">true: Change the trace settings with these parameters.</param>
        public static void Initialize(string TraceLocation, string TraceCategory, string FileExtensionWithoutPeriod, int TraceLevel, bool LogErrors, bool willForceReset = false)
        {
            try
            {
                if (TraceLocation == null)
                {
                    throw new NullReferenceException("TraceLocation must not be null");
                }
                if (TraceCategory == null)
                {
                    throw new NullReferenceException("TraceCategory must not be null");
                }
                if (FileExtensionWithoutPeriod == null)
                {
                    throw new NullReferenceException("FileExtensionWithoutPeriod must not be null");
                }

                lock (_instanceLocker)
                {
                    // Save the settings if we should.
                    if (willForceReset || (_traceLocation == null))
                    {
                        _maxPriority = TraceLevel;
                        _fileExtensionWithoutPeriod = FileExtensionWithoutPeriod;
                        _traceCategory = TraceCategory;
                        _traceLocation = TraceLocation;
                        _logErrors = LogErrors;
                        Instance.writeToLog(1, "CLSptTrace: Initialize: Trace initialized, TraceLevel: {0}. Extension: {1}. Category: {2}. Dir: {3}.", 
                                TraceLevel, FileExtensionWithoutPeriod, TraceCategory, TraceLocation);
                    }
                    else
                    {
                        Instance.writeToLog(1, "CLSptTrace: Initialize: Trace already initialized.");
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// The single instance method that writes to the log file
        /// </summary>
        /// <param name="priority">The priority of the message.  0 is highest.</param>
        /// <param name="message">The message to write to the log</param>
        public void writeToLog(int priority, string format, params object[] args)
        {
            try
            {
                // Only write high priority messages
                if ((_traceLocation == null || priority <= _maxPriority)
                    
                    // only trace if trace category was set via initialization to prevent an exception being thrown -David
                    && _traceCategory != null)
                {
                    string logFilePath = Helpers.CheckLogFileExistance(TraceLocation: _traceLocation, SyncBoxId: null, UserDeviceId: null, TraceCategory: _traceCategory, 
                            FileExtensionWithoutPeriod: _fileExtensionWithoutPeriod, OnNewTraceFile: null, OnPreviousCompletion: null);

                    int formatParamCount = Regex.Matches(format,
                        @"\{\d+[^\{\}]*\}",
                        RegexOptions.Compiled
                            | RegexOptions.CultureInvariant).Count;

                    if (args == null)
                    {
                        args = new object[0];
                    }

                    object[] copiedArgs;
                    if (args.Length == formatParamCount)
                    {
                        copiedArgs = args;
                    }
                    else
                    {
                        copiedArgs = new object[formatParamCount];
                        if (args.Length > formatParamCount)
                        {
                            Array.Copy(args, copiedArgs, formatParamCount);
                        }
                        else // args.Length < formatParamCount
                        {
                            Array.Copy(args, copiedArgs, args.Length);

                            // example:
                            // 3 args (0, 1, 2)
                            // 5 format params (0 through 4)
                            // start at 3, go to 4
                            for (int missingArgument = args.Length; missingArgument < formatParamCount; missingArgument++)
                            {
                                copiedArgs[missingArgument] = "¡¡MissingArg" + missingArgument.ToString() + "!!";
                            }
                        }
                    }

                    // Lock while writing to prevent contention for the log file
                    lock (Helpers.LogFileLocker)
                    {
                        // Format the string
                        string message = string.Format(format, copiedArgs);

                        // Create the entry
                        LogMessage logEntry = new LogMessage(priority, message);

                        // Log to the VS output window, or to DbgView
                        //Debug.WriteLine(string.Format("{0}_{1}_{2}_{3}", logEntry.LogTime, logEntry.ProcessId.ToString("x"), logEntry.ThreadId.ToString("x"), logEntry.Message));

                        // This could be optimised to prevent opening and closing the file for each write
                        using (FileStream fs = File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                        {
                            using (StreamWriter log = new StreamWriter(fs))
                            {
                                log.WriteLine(string.Format("{0}_{1}_{2}_{3}", logEntry.LogTime, logEntry.ProcessId.ToString("x"), logEntry.ThreadId.ToString("x"), logEntry.Message));
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// A Log class to store the message and the Date and Time the log entry was created
        /// </summary>
        private class LogMessage
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
    }

    #endregion
}
