using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SyncTestServer
{
    public interface IServerData : INotifyPropertyChanged
    {
        void InitializeServer(Model.ScenarioServer initialData);
    }
}