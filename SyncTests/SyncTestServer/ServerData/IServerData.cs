using SyncTestServer.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        IEnumerable<CloudApiPublic.JsonContracts.Event> GrabEventsAfterLastSync(CloudApiPublic.JsonContracts.Push request, User currentUser, long newSyncId);
    }
}