using CloudApiPublic.Model;
using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class CreateFolderResponseEventArgs : CreateFolderEventArgs
    {
        public string ResponseText { get; set; }
        public FileChange FileChange { get; set; }
        public CLHttpRestStatus RestStatus { get; set; }
        public CloudApiPublic.JsonContracts.Event ReturnEvent { get; set; }

        public CreateFolderResponseEventArgs(CreateFolderEventArgs baseInput)
        { 
            this.boxCreationStatus = baseInput.boxCreationStatus;
            this.CreationTime = baseInput.CreationTime;
            this.CreateTaskDirectoryInfo = baseInput.CreateTaskDirectoryInfo;
            this.Creds = baseInput.Creds;
            this.CredsStatus = baseInput.CredsStatus;
            this.CurrentTask = baseInput.CurrentTask;
            this.ProcessingErrorHolder = baseInput.ProcessingErrorHolder;
            this.SyncBox = baseInput.SyncBox;
        }


        public CreateFolderResponseEventArgs(CreateFolderEventArgs baseInput, FileChange change, string response, CLHttpRestStatus restStatus, CloudApiPublic.JsonContracts.Event returnEvent)
        {
            this.boxCreationStatus = baseInput.boxCreationStatus;
            this.CreationTime = baseInput.CreationTime;
            this.CreateTaskDirectoryInfo = baseInput.CreateTaskDirectoryInfo;
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
