//
// ISyncDataObject.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Interfaces
{
    public interface ISyncDataObject
    {
        CLError grabChangesFromFileSystemMonitor(IEnumerable<PossiblyPreexistingFileChangeInError> initialFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<PossiblyPreexistingFileChangeInError> outputChangesInError);

        CLError mergeToSql(IEnumerable<FileChangeMerge> mergeToFroms,
            bool alreadyObtainedLock = false);

        CLError addChangesToProcessingQueue(IEnumerable<FileChange> toAdd,
            bool insertAtTop,
            GenericHolder<List<FileChange>> errorHolder);

        CLError completeSyncSql(string syncId,
            IEnumerable<long> syncedEventIds,
            out long syncCounter,
            string newRootPath = null);

        string getLastSyncId { get; }

        CLError dependencyAssignment(IEnumerable<PossiblyStreamableFileChange> toAssign,
            IEnumerable<FileChange> currentFailures,
            out IEnumerable<PossiblyStreamableFileChange> outputChanges,
            out IEnumerable<FileChange> outputFailures);

        CLError applySyncFromChange(FileChange toApply);

        CLError completeSingleEvent(long eventId);

        CLError getMetadataByPathAndRevision(string path, string revision, out FileMetadata metadata);
    }
}