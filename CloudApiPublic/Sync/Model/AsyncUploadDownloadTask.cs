//
// AsyncUploadDownloadTask.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Sync.Model
{
    internal struct AsyncUploadDownloadTask
    {
        public SyncDirection Direction
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid AsyncUploadDownloadTask");
                }
                return _direction;
            }
        }
        private SyncDirection _direction;

        public Task<EventIdAndCompletionProcessor> Task
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid AsyncUploadDownloadTask");
                }
                return _task;
            }
        }
        private Task<EventIdAndCompletionProcessor> _task;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private bool _isValid;

        public AsyncUploadDownloadTask(SyncDirection Direction, Task<EventIdAndCompletionProcessor> Task)
        {
            if (Task == null)
            {
                throw new NullReferenceException("Task cannot be null");
            }

            this._direction = Direction;
            this._task = Task;
            this._isValid = true;
        }
    }
}