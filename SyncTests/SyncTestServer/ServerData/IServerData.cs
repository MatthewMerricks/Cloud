//
// IServerData.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public interface IServerData
    {
        void InitializeServer(Model.ScenarioServer initialData, Action userWasNotLockedDetected = null);
        User FindUserByAKey(string akey, out Device specificDevice);
        IEnumerable<CloudApiPublic.JsonContracts.File> PurgePendingFiles(User currentUser, CloudApiPublic.JsonContracts.PurgePending request, out bool deviceNotInUser);
        long NewSyncIdBeforeStart { get; }
        IEnumerable<CloudApiPublic.JsonContracts.Event> GrabEventsAfterLastSync(string lastSyncIdString, string relativeRootPath, User currentUser, long newSyncId);
        void ApplyClientEventToServer(long syncId, User currentUser, Device currentDevice, CloudApiPublic.JsonContracts.Event toEvent);
        bool WriteUpload(Stream toWrite, string storageKey, long contentLength, string contentMD5, User currentUser, bool disposeStreamAfterWrite = true);
        Stream GetDownload(string storageKey, User currentUser, out long fileSize);
    }
}