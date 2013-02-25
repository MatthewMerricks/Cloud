using CloudApiPublic.Model;
using CloudApiPublic.Static;
using CloudSDK_SmokeTest.Events.CLEventArgs;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.ManagerEventArgs
{
    public class SmokeTestManagerEventArgs: TaskEventArgs
    {
        public FileInfo FileInfo { get; set; }
        public DirectoryInfo DirectoryInfo { get; set; }
        public DateTime CurrentTime { get; set; }
        public DirectoryInfo RootDirectory { get; set; }
        public CloudApiPublic.JsonContracts.Session Session { get; set; }
        public long PlanID { get; set; }
        public CLHttpRestStatus RestStatus { get; set; }


        public SmokeTestManagerEventArgs(){}

        public SmokeTestManagerEventArgs(TaskEventArgs inputArgs)
        {
            this.boxCreationStatus = inputArgs.boxCreationStatus;
            this.Creds = inputArgs.Creds;
            this.CredsStatus = inputArgs.CredsStatus;
            this.CurrentTask = inputArgs.CurrentTask;
            this.ParamSet = inputArgs.ParamSet;
            this.SyncBox = inputArgs.SyncBox;
            this.Creds = inputArgs.Creds;
            this.ProcessingErrorHolder = inputArgs.ProcessingErrorHolder;
        }

        public SmokeTestManagerEventArgs(InputParams paramSet, SmokeTask smokeTask, GenericHolder<CLError> ProcessingErrorHolder)
        {
            this.ParamSet = paramSet;
            this.CurrentTask = smokeTask;
            this.ProcessingErrorHolder = ProcessingErrorHolder;
            this.RootDirectory = new DirectoryInfo(paramSet.ManualSync_Folder.Replace("\"", ""));
        }
    }
}
