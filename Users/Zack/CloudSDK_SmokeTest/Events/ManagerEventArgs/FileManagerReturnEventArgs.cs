using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.ManagerEventArgs
{
    public class FileManagerReturnEventArgs : SmokeTestManagerEventArgs
    {
        public FileChange FileChange { get; set; }
        public CloudApiPublic.JsonContracts.Event ReturnEvent {get;set;}
        public FileManagerReturnEventArgs() { }
        public FileManagerReturnEventArgs(SmokeTestManagerEventArgs e)
            : base((e as TaskEventArgs))
        {
            this.FileInfo = e.FileInfo;
            this.PlanID = e.PlanID;
            this.RestStatus = e.RestStatus;
            this.Session = e.Session;
        }
    }
}
