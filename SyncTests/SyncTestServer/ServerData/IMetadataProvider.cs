//
// IMetadataProvider.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public interface IMetadataProvider
    {
        void InitializeProvider(IServerStorage storage, IEnumerable<InitialMetadata> initialData = null, Action userWasNotLockedDetected = null);
        long NewSyncIdBeforeStart { get; }
        bool AddFolderMetadata(long syncId, int userId, FilePath relativePathFromRoot, FileMetadata metadata);
        bool AddFileMetadata(long syncId, int userId, Guid deviceId, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, byte[] MD5);
        bool TryGetMetadata(int userId, FilePath relativePathFromRoot, out FileMetadata metadata);
        bool RecursivelyRemoveMetadata(long syncId, int userId, FilePath relativePathFromRoot);
        bool RecursivelyRenameMetadata(long syncId, int userId, FilePath relativePathFrom, FilePath relativePathTo);
        bool UpdateMetadata(long syncId, int userId, Guid deviceId, string revision, FilePath relativePathFromRoot, FileMetadata metadata, out bool isPending, out bool newUpload, out bool conflict, byte[] MD5 = null);
        IEnumerable<FileChange> ChangesSinceSyncId(long syncId, int userId);
        IEnumerable<KeyValuePair<FilePath, FileMetadata>> PurgeUserPendingsByDevice(int userId, Guid deviceId);
    }
}