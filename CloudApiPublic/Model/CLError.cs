//
//  CLError.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CloudApiPublic.Model
{
    public class CLError
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
        public const string ErrorInfo_Sync_Run_Status = "SyncRunStatus";

        public string errorDomain;
        public string errorDescription;
        public int errorCode;
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
            errorDomain = "";
            errorDescription = "";
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

        public static CLError operator +(CLError err, FileStream fStream)
        {
            if (err == null)
            {
                return ((CLError)new Exception("FileStream added first instead of exception")) + fStream;
            }

            err.AddFileStream(fStream);
            return err;
        }

        public void AddFileStream(FileStream fStream)
        {
            this.errorInfo.Add(CLError.ErrorInfo_FileStreamToDispose +
                    this.errorInfo.Count(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_FileStreamToDispose)).ToString(),
                fStream);
        }

        public void AddException(Exception ex, bool replaceErrorDescription = false)
        {
            this.errorInfo.Add(CLError.ErrorInfo_Exception +
                    this.errorInfo.Count(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_Exception)).ToString(),
                ex);
            if (replaceErrorDescription)
            {
                this.errorDescription = ex.Message;
            }
        }

        public IEnumerable<FileStream> DequeueFileStreams()
        {
            FileStream tryCast;
            Func<KeyValuePair<string, object>, Func<string, bool>, FileStream> removeAndReturnValue = (currentPair, removeAction) =>
                {
                    try
                    {
                        removeAction(currentPair.Key);
                    }
                    catch
                    {
                    }
                    return currentPair.Value as FileStream;
                };
            return this.errorInfo.Where(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_FileStreamToDispose)
                    && (tryCast = currentPair.Value as FileStream) != null)
                .Select(currentPair => removeAndReturnValue(currentPair, this.errorInfo.Remove));
        }

        public static IEnumerable<FileStream> DequeueFileStreams(CLError err)
        {
            if (err == null)
            {
                return new FileStream[0];
            }
            return err.DequeueFileStreams();
        }

        public Exception GrabFirstException()
        {
            object dictionaryValue;
            if (this.errorInfo.TryGetValue(CLError.ErrorInfo_Exception, out dictionaryValue))
            {
                return dictionaryValue as Exception;
            }
            return null;
        }

        public static void LogErrors(CLError err, string logLocation, bool loggingEnabled, bool clearFileStreams = true)
        {
            if (err != null)
            {
                err.LogErrors(logLocation, loggingEnabled, clearFileStreams);
            }
        }

        public void LogErrors(string logLocation, bool loggingEnabled, bool clearFileStreams = true)
        {
            if (loggingEnabled
                && !string.IsNullOrWhiteSpace(logLocation))
            {
                try
                {
                    FileInfo logFile = new FileInfo(logLocation);

                    if (!logFile.Directory.Exists)
                    {
                        logFile.Directory.Create();
                    }

                    List<DateTime> logDates = new List<DateTime>();
                    DateTime currentDate = DateTime.UtcNow.Date;
                    bool currentDateFound = false;

                    foreach (FileInfo currentFile in logFile.Directory.EnumerateFiles())
                    {
                        if (currentFile.Name.StartsWith(logFile.Name)
                            && currentFile.Name.Length == (logFile.Name.Length + 12))//12 is the sum of 4 and 8; 4 is the length of ".txt", the file extension; 8 is the length of the date time format YYYYMMDD
                        {
                            string nameDatePortion = currentFile.Name.Substring(logFile.Name.Length, currentFile.Name.Length - logFile.Name.Length - 4); //4 is the length of ".txt", the file extension
                            
                            // the date portion of the log name should be in the format YYYYMMDD

                            int nameDateYear;
                            if (int.TryParse(nameDatePortion.Substring(0, 4), out nameDateYear))
                            {
                                int nameDateMonth;
                                if (int.TryParse(nameDatePortion.Substring(4, 2), out nameDateMonth))
                                {
                                    int nameDateDay;
                                    if (int.TryParse(nameDatePortion.Substring(6), out nameDateDay))
                                    {
                                        try
                                        {
                                            DateTime nameDate = new DateTime(nameDateYear, nameDateMonth, nameDateDay, currentDate.Hour, currentDate.Minute, currentDate.Second, DateTimeKind.Utc);
                                            if (nameDate.Year == currentDate.Year
                                                && nameDate.Month == currentDate.Month
                                                && nameDate.Day == currentDate.Day)
                                            {
                                                currentDateFound = true;
                                            }
                                            logDates.Add(nameDate);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (logDates.Count > (currentDateFound ? 10 : 9))
                    {
                        foreach (DateTime logToRemove in logDates.OrderByDescending(orderDate => orderDate.Ticks).Skip(currentDateFound ? 10 : 9))
                        {
                            string currentDeletePath = logFile.FullName +
                                logToRemove.ToString("yyyyMMdd") + // formats to YYYYMMDD
                                ".txt";
                            try
                            {
                                File.Delete(currentDeletePath);
                            }
                            catch
                            {
                            }
                        }
                    }

                    try
                    {
                        FileInfo currentLogFile = new FileInfo(logFile.FullName +
                                currentDate.ToString("yyyyMMdd") + // formats to YYYYMMDD
                                ".txt");
                        if (!currentDateFound)
                        {
                            currentLogFile.Create();
                        }
                        using (StreamWriter logWriter = currentLogFile.AppendText())
                        {
                            if (this.errorInfo.ContainsKey(CLError.ErrorInfo_Sync_Run_Status))
                            {
                                logWriter.WriteLine(this.errorInfo[CLError.ErrorInfo_Sync_Run_Status]);
                            }
                            logWriter.WriteLine(this.errorDescription);
                            foreach (object currentException in this.errorInfo
                                .Where(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_Exception))
                                .Select(currentPair => currentPair.Value))
                            {
                                Exception castException = currentException as Exception;
                                if (castException != null)
                                {
                                    int tabCounter = 1;
                                    Func<int, string> makeTabs = tabCount =>
                                        new string(Enumerable.Range(0, 4 * tabCount)
                                            .Select(currentTabSpace => ' ')
                                            .ToArray());
                                    while (castException != null)
                                    {
                                        logWriter.WriteLine(
                                            makeTabs(tabCounter) +
                                            castException.Message);

                                        castException = castException.InnerException;
                                        tabCounter++;
                                    }
                                }
                            }
                            logWriter.WriteLine();
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
