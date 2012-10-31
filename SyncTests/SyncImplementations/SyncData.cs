using CloudApiPublic.Interfaces;
using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SyncTests.SyncImplementations
{
    public sealed class SyncData : ISyncDataObject
    {
        #region singleton pattern
        public static SyncData Instance
        {
            get
            {
                lock (InstanceLocker)
                {
                    return _instance
                        ?? (_instance = new SyncData());
                }
            }
        }
        private static SyncData _instance = null;
        private static readonly object InstanceLocker = new object();

        private SyncData() { }
        #endregion

        public CLError grabChangesFromFileSystemMonitor(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError)
        {
            outputChanges = Enumerable.Empty<PossiblyStreamableFileChange>();
            outputChangesInError = Enumerable.Empty<PossiblyPreexistingFileChangeInError>();
            return null;
        }

        public CLError mergeToSql(IEnumerable<FileChangeMerge> mergeToFroms,
            bool alreadyObtainedLock = false)
        {
            return null;
        }

        public CLError addChangesToProcessingQueue(IEnumerable<FileChange> toAdd,
            bool insertAtTop,
            GenericHolder<List<FileChange>> errorHolder)
        {
            return null;
        }

        private long syncCounter = 0;
        public CLError completeSyncSql(string syncId,
            IEnumerable<long> syncedEventIds,
            out long syncCounter,
            string newRootPath = null)
        {
            syncCounter = Interlocked.Increment(ref this.syncCounter);
            return null;
        }

        public string getLastSyncId
        {
            get
            {
                return CLDefinitions.CLDefaultSyncID;
            }
        }

        public CLError dependencyAssignment(IEnumerable<PossiblyStreamableFileChange> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<FileChange> outputFailures)
        {
            outputChanges = toAssign;
            outputFailures = currentFailures;
            return null;
        }

        public CLError applySyncFromChange(FileChange toApply)
        {
            return null;
        }

        public CLError completeSingleEvent(long eventId)
        {
            return null;
        }

        public CLError getMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata)
        {
            throw new NotImplementedException();
        }
    }
}