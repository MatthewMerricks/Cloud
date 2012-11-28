//
//  CLError.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Class that represents an error, and supports logging of the error.  The error may contain multiple exceptions.
    /// </summary>
    /// <include file='filename' path='tagpath[@name="id"]' />
    public sealed class CLError
    {
        // Common error codes
        public enum ErrorCodes : int
        {
            Exception = 9999
        }

        // Error domains
        public const string ErrorDomain_Application = "Cloud";

        // errorInfo keys
        public const string ErrorInfo_Exception = "Exception";
        public const string ErrorInfo_FileStreamToDispose = "FileStream";
        /// <summary>
        /// errorInfo key for the last recorded status in the Run method in Sync class
        /// </summary>
        public const string ErrorInfo_Sync_Run_Status = "SyncRunStatus";

        public string errorDomain;
        public string errorDescription;
        public int errorCode;
        /// <summary>
        /// Returns the dictionary of infoes for the current CLError;
        /// it is never null
        /// </summary>
        // Converted to property with only a getter to prevent any possibility of null reference errors
        // -David
        public Dictionary<string, object> errorInfo
        {
            get
            {
                if (_errorInfo == null)
                {
                    _errorInfo = new Dictionary<string, object>();
                }
                return _errorInfo;
            }
        }
        private Dictionary<string, object> _errorInfo = null;

        public CLError()
        {
            errorDomain = string.Empty;
            errorDescription = string.Empty;
            errorCode = 0;
        }

        public static implicit operator CLError(Exception ex)
        {
            return new CLError()
            {
                errorCode = (int)CLError.ErrorCodes.Exception,
                errorDescription = ex.Message,
                errorDomain = CLError.ErrorDomain_Application,
                _errorInfo = new Dictionary<string, object>()
                {
                    { CLError.ErrorInfo_Exception, ex }
                }
            };
        }

        public static CLError operator +(CLError err, Exception ex)
        {
            if (err == null)
            {
                return ex;
            }

            err.AddException(ex);
            return err;
        }

        // Added so we can append FileStream objects which can later be disposed upon error handling
        // -David
        public static CLError operator +(CLError err, Stream fStream)
        {
            if (fStream == null)
            {
                return err;
            }
            if (err == null)
            {
                return ((CLError)new Exception(StreamFirstMessage)) + fStream;
            }

            err.AddFileStream(fStream);
            return err;
        }
        public const string StreamFirstMessage = "FileStream added first instead of exception";

        // Added so we can append FileStream objects which can later be disposed upon error handling
        // -David
        public void AddFileStream(Stream fStream)
        {
            this.errorInfo.Add(CLError.ErrorInfo_FileStreamToDispose +
                    this.errorInfo.Count(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_FileStreamToDispose)).ToString(),
                fStream);
        }

        public void AddException(Exception ex, bool replaceErrorDescription = false)
        {
            if (ex != null)
            {
                this.errorInfo.Add(CLError.ErrorInfo_Exception +
                        this.errorInfo.Count(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_Exception)).ToString(),
                    ex);
                if (replaceErrorDescription)
                {
                    this.errorDescription = ex.Message;
                }
            }
        }

        /// <summary>
        /// Takes all the FileStream instances out of this error and returns them for disposal
        /// </summary>
        /// <returns>Returns dequeued FileStream objects</returns>
        public IEnumerable<Stream> DequeueStreams()
        {
            Stream tryCast;
            Func<KeyValuePair<string, object>, Func<string, bool>, Stream> removeAndReturnValue = (currentPair, removeAction) =>
                {
                    try
                    {
                        removeAction(currentPair.Key);
                    }
                    catch
                    {
                    }
                    return currentPair.Value as Stream;
                };
            return this.errorInfo.Where(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_FileStreamToDispose)
                    && (tryCast = currentPair.Value as Stream) != null)
                .ToArray()
                .Select(currentPair => removeAndReturnValue(currentPair, this.errorInfo.Remove));
        }

        /// <summary>
        /// Takes all the FileStream instances out of a CLError and returns them for disposal;
        /// returns an empty array for null input
        /// </summary>
        /// <param name="err">CLError for FileStream dequeue</param>
        /// <returns>Returns dequeued FileStream objects</returns>
        public static IEnumerable<Stream> DequeueFileStreams(CLError err)
        {
            if (err == null)
            {
                return new FileStream[0];
            }
            return err.DequeueStreams();
        }

        /// <summary>
        /// Returns the first exception contained in errorInfo within a CLError;
        /// null for null input
        /// </summary>
        /// <param name="err">CLError for pulling exception</param>
        /// <returns>Returns pulled exception</returns>
        public static Exception GrabFirstException(CLError err)
        {
            if (err == null)
            {
                return null;
            }
            return err.GrabFirstException();
        }

        /// <summary>
        /// Returns the first exception contained in errorInfo within this CLError
        /// </summary>
        /// <returns>Returns pulled exception</returns>
        public Exception GrabFirstException()
        {
            object dictionaryValue;
            if (this.errorInfo.TryGetValue(CLError.ErrorInfo_Exception, out dictionaryValue))
            {
                return dictionaryValue as Exception;
            }
            return null;
        }

        /// <summary>
        /// Returns all exceptions contained in the errorInfo within a CLError;
        /// empty array for null input
        /// </summary>
        /// <param name="err">CLError for pullling exceptions</param>
        /// <returns>Returns pulled exceptions</returns>
        public static IEnumerable<Exception> GrabExceptions(CLError err)
        {
            if (err == null)
            {
                return Enumerable.Empty<Exception>();
            }
            return err.GrabExceptions();
        }

        /// <summary>
        /// Returns all exceptions contained in the errorInfo of this CLError
        /// </summary>
        /// <returns>Returns pulled exceptions</returns>
        public IEnumerable<Exception> GrabExceptions()
        {
            List<Exception> toReturn = new List<Exception>();
            foreach (Exception currentValue in this.errorInfo.Where(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_Exception))
                .Select(currentPair => currentPair.Value))
            {
                Exception castValue = currentValue as Exception;
                if (castValue != null)
                {
                    toReturn.Add(castValue);
                }
            }
            return toReturn;
        }

        /// <summary>
        /// Logs error information to the specified location if loggingEnabled parameter is passed as true for a CLError
        /// </summary>
        /// <param name="err">CLError to log</param>
        /// <param name="logLocation">Base location for log files before date and extention are appended</param>
        /// <param name="loggingEnabled">Determines whether logging will actually occur</param>
        public static void LogErrors(CLError err, string logLocation, bool loggingEnabled)
        {
            if (err != null)
            {
                err.LogErrors(logLocation, loggingEnabled);
            }
        }

        /// <summary>
        /// Logs error information to the specified location if loggingEnabled parameter is passed as true for this CLError
        /// </summary>
        /// <param name="logLocation">Base location for log files before date and extention are appended</param>
        /// <param name="loggingEnabled">Determines whether logging will actually occur</param>
        [MethodImpl(MethodImplOptions.Synchronized)] // synchronized so multiple logs don't try writing to the files simultaneously
        public void LogErrors(string logLocation, bool loggingEnabled)
        {
            // skip logging if it is either disabled or the location of the log is invalid
            if (loggingEnabled
                && !string.IsNullOrWhiteSpace(logLocation))
            {
                try
                {
                    // Determine the full path of the trace file to use and manage the number of old trace files.
                    string logFilePath = Helpers.CheckLogFileExistance(TraceLocation: logLocation, UniqueUserId: null, UserDeviceId: null, TraceCategory: "CloudError",
                        FileExtensionWithoutPeriod: "log", OnNewTraceFile: null, OnPreviousCompletion: null);

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

                                // for each custom error key status,
                                // if the key/value pair exists in errorInfo then write them first to the log
                                #region custom error key statuses
                                if (this.errorInfo.ContainsKey(CLError.ErrorInfo_Sync_Run_Status))
                                {
                                    logWriter.WriteLine(this.errorInfo[CLError.ErrorInfo_Sync_Run_Status]);
                                }
                                #endregion

                                // write the message of this error to the log
                                logWriter.WriteLine(this.errorDescription);

                                // pull out the values from the errorInfo key/value pairs whose keys start with the exception name
                                foreach (KeyValuePair<string, object> currentException in this.errorInfo
                                    .Where(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_Exception)))
                                {
                                    // try cast the current value as an exception
                                    Exception castException = currentException.Value as Exception;
                                    if (castException != null)
                                    {
                                        // define a string for storing the StackTrace, defaulting to null
                                        string stack = null;
                                        // I don't know if it's dangerous to pull out the StackTrace, so I wrap it safely
                                        try
                                        {
                                            stack = castException.StackTrace;
                                        }
                                        catch
                                        {
                                        }

                                        // keep track of how many inner exceptions have recursed to increase tab amount
                                        int tabCounter = 1;
                                        // define function to build spaces by tab count
                                        Func<int, string> makeTabs = tabCount =>
                                            new string(Enumerable.Range(0, 4 * tabCount)// the "4 *" multiplier means each tab is 4 spaces
                                                .Select(currentTabSpace => ' ')// components of the tab are spaces
                                                .ToArray());

                                        // determines if this exception message was already written by writing errorDescription
                                        bool foundSameMessage = currentException.Key == CLError.ErrorInfo_Exception
                                            && castException.Message == this.errorDescription;

                                        // recurse through inner exceptions, each time with an extra tab appended
                                        while (castException != null)
                                        {
                                            if (foundSameMessage)
                                            {
                                                foundSameMessage = false;
                                            }
                                            else
                                            {
                                                // write the current exception message to the log after the tabCounter worth of tabs
                                                logWriter.WriteLine(
                                                    makeTabs(tabCounter) +
                                                    castException.Message);

                                                tabCounter++;
                                            }

                                            // prepare for next inner exception recursion
                                            castException = castException.InnerException;
                                        }

                                        // if a StackTrace was found,
                                        // then write it to the log
                                        if (!string.IsNullOrWhiteSpace(stack))
                                        {
                                            // write the StackTrace to the log with 1 tab
                                            logWriter.WriteLine(
                                                makeTabs(1) + "StackTrace:" + Environment.NewLine +
                                                stack);
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
        private Nullable<DateTime> LastDayLogCreated = null;
    }
}