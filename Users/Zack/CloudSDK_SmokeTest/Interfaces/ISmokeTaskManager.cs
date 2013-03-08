using CloudSDK_SmokeTest.Events.ManagerEventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Interfaces
{
    public interface ISmokeTaskManager
    {
        int Create(SmokeTestManagerEventArgs e);
        int Rename(SmokeTestManagerEventArgs e);
        int Delete(SmokeTestManagerEventArgs e);
        int UnDelete(SmokeTestManagerEventArgs e);
        int Download(SmokeTestManagerEventArgs e);
        int ListItems(SmokeTestManagerEventArgs e);
        int AlternativeAction(SmokeTestManagerEventArgs e);
    }
}
