using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public sealed class ItemListHelperEventArgs : TaskEventArgs
    {

        public ListItems ListItemsTask { get; set; }

        public ItemListHelperEventArgs() { }

        public ItemListHelperEventArgs(TaskEventArgs inputArgs)
        {
            this.boxCreationStatus = inputArgs.boxCreationStatus;
            this.Creds = inputArgs.Creds;
            this.CredsStatus = inputArgs.CredsStatus;
            this.CurrentTask = inputArgs.CurrentTask;
            this.ParamSet = inputArgs.ParamSet;
            this.SyncBox = inputArgs.SyncBox;
            this.ReportBuilder = inputArgs.ReportBuilder;
        }
    }
}
