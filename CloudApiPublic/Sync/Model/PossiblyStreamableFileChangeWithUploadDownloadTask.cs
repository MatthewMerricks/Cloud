//
// PossiblyStreamableFileChangeWithUploadDownloadTask.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Sync.Model
{
    internal struct PossiblyStreamableFileChangeWithUploadDownloadTask
    {
        public PossiblyStreamableFileChange FileChange
        {
            get
            {
                if (!_fileChange.IsValid
                    || !_task.IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithUploadDownloadTask");
                }
                return _fileChange;
            }
        }
        private PossiblyStreamableFileChange _fileChange;

        public AsyncUploadDownloadTask Task
        {
            get
            {
                if (!_fileChange.IsValid
                    || !_task.IsValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid PossiblyStreamableFileChangeWithUploadDownloadTask");
                }
                return _task;
            }
        }
        private AsyncUploadDownloadTask _task;

        public PossiblyStreamableFileChangeWithUploadDownloadTask(PossiblyStreamableFileChange FileChange, AsyncUploadDownloadTask Task)
        {
            this._fileChange = FileChange;
            this._task = Task;
        }
    }
}