using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class CreateFileResponseEventArgs : CreateFileEventArgs
    {
        public string ResponseText { get; set; }
        public FileChange FileChange { get; set; }
        public CLHttpRestStatus RestStatus { get; set; }
        public CloudApiPublic.JsonContracts.Event ReturnEvent { get; set; }

        public CreateFileResponseEventArgs(CreateFileEventArgs baseInput)
        {
            this.boxCreationStatus = baseInput.boxCreationStatus;
            this.CreateCurrentTime = baseInput.CreateCurrentTime;
            this.CreateTaskFileInfo = baseInput.CreateTaskFileInfo;
            this.Creds = baseInput.Creds;
            this.CredsStatus = baseInput.CredsStatus;
            this.CurrentTask = baseInput.CurrentTask;
            this.ProcessingErrorHolder = baseInput.ProcessingErrorHolder;
            this.SyncBox = baseInput.SyncBox;
        }

        public CreateFileResponseEventArgs(CreateFileEventArgs baseInput, FileChange change, string response, CLHttpRestStatus restStatus, CloudApiPublic.JsonContracts.Event returnEvent)
        {
            this.boxCreationStatus = baseInput.boxCreationStatus;
            this.CreateCurrentTime = baseInput.CreateCurrentTime;
            this.CreateTaskFileInfo = baseInput.CreateTaskFileInfo;
            this.Creds = baseInput.Creds;
            this.CredsStatus = baseInput.CredsStatus;
            this.CurrentTask = baseInput.CurrentTask;
            this.ProcessingErrorHolder = baseInput.ProcessingErrorHolder;
            this.SyncBox = baseInput.SyncBox;
            this.FileChange = change;
            this.ResponseText = response;
            this.RestStatus = restStatus;
            this.ReturnEvent = returnEvent;
        }
    }
}
