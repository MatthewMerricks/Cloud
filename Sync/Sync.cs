using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using CloudApiPrivate.Model.Settings;
using CloudApiPublic.Model;
using CloudApiPublic.Support;

namespace Sync
{
    public static class Sync
    {
        private static ProcessingQueuesTimer failureTimer
        {
            get
            {
                lock (failureTimerLocker)
                {
                    if (_failureTimer == null)
                    {
                        CLError timerError = ProcessingQueuesTimer.CreateAndInitializeProcessingQueuesTimer(FailureProcessing,
                            5000,// wait five seconds between processing
                            out _failureTimer);
                        if (timerError != null)
                        {
                            throw timerError.GrabFirstException();
                        }
                    }
                    return _failureTimer;
                }
            }
        }
        private static ProcessingQueuesTimer _failureTimer = null;
        private static object failureTimerLocker = new object();

        // need private queue for failures here

        private static void FailureProcessing()
        {
            // Insert into top of queued changes in FileMonitor, start its timer
        }

        private static CLError DisposeAllStreams(this IEnumerable<FileStream> allStreams)
        {
            CLError disposalError = null;
            if (allStreams != null)
            {
                foreach (FileStream currentStream in allStreams)
                {
                    try
                    {
                        currentStream.Dispose();
                    }
                    catch (Exception ex)
                    {
                        disposalError += ex;
                    }
                }
            }
            return disposalError;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        /// <summary>
        /// Primary method for all syncing (both From and To),
        /// synchronized so only a single thread can access it at a time
        /// </summary>
        /// <returns>Returns errors that occurred while syncing, if any</returns>
        public static CLError Run()
        {
            CLError toReturn = null;
            string syncStatus = "Sync Run entered";
            try
            {
                // Grab processed changes (will open locked FileStreams for all file adds/modifies)

                syncStatus = "Sync Run grabbed processed changes";

                // Synchronously or asynchronously fire off all events that have a storage key (MDS events)
                // If a completed event has dependencies, process accordingly

                syncStatus = "Sync Run initial synchronous operations complete";

                // Within a lock on the failure queue (failureTimer.TimerRunningLocker),
                // check if each current event needs to be moved to a dependency under a failure event or an event in the current batch

                syncStatus = "Sync Run initial dependencies calculated";

                // Take events without dependencies that were not fired off in order to perform communication (or Sync From for no events left)

                syncStatus = "Sync Run communication complete";

                // Merge in server values into DB (storage key, revision, etc) and add new Sync From events

                syncStatus = "Sync Run server values merged into database";

                // Within a lock on the failure queue (failureTimer.TimerRunningLocker),
                // check if each current server action needs to be moved to a dependency under a failure event or a server action in the current batch

                syncStatus = "Sync Run post-communication dependencies calculated";

                // Synchronously complete all local operations without dependencies (exclude file upload/download) and record successful events;
                // If a completed event has dependencies, stick them on the end of the current batch;
                // If an event fails to complete, stick it on the failure queue and start its timer

                syncStatus = "Sync Run synchronous post-communication operations complete";

                // Write new Sync point to database with succesful events

                syncStatus = "Sync Run new sync point persisted";

                // Asynchronously fire off all remaining upload/download operations without dependencies

                // No need to update syncStatus here unless we have a trace of all operations
            }
            catch (Exception ex)
            {
                toReturn += ex;
                toReturn.errorInfo.Add(CLError.ErrorInfo_Sync_Run_Status, syncStatus);
                CLError disposalError = toReturn.DequeueFileStreams().DisposeAllStreams();
                foreach (Exception disposalException in CLError.GrabExceptions(disposalError))
                {
                    toReturn += ex;
                }
                toReturn.LogErrors(Settings.Instance.ErrorLogLocation, Settings.Instance.LogErrors);
                return toReturn;
            }
            return null;
        }
    }
}