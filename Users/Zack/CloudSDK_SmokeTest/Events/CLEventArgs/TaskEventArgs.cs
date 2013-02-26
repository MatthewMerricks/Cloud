using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class TaskEventArgs : CLEventArgs
    {
        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }
        public SmokeTask CurrentTask { get; set; }
        public StringBuilder ReportBuilder { get; set; }


        public TaskEventArgs() { }
        public TaskEventArgs(TaskEventArgs input)
        {
            this.boxCreationStatus = input.boxCreationStatus;
            this.Creds = input.Creds;
            this.CredsStatus = input.CredsStatus;
            this.CurrentTask = input.CurrentTask;
            this.ParamSet = input.ParamSet;
            this.ProcessingErrorHolder = input.ProcessingErrorHolder;
            this.SyncBox = input.SyncBox;
            this.ReportBuilder = input.ReportBuilder;
        }
    }
}
