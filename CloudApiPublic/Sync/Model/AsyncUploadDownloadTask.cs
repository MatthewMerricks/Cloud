//
// AsyncUploadDownloadTask.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Sync.Model
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
        private readonly SyncDirection _direction;

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
        private readonly Task<EventIdAndCompletionProcessor> _task;

        public Guid ThreadId
        {
            get
            {
                if (!_isValid)
                {
                    throw new ArgumentException("Cannot retrieve property values on an invalid AsyncUploadDownloadTask");
                }
                return _threadId;
            }
        }
        private readonly Guid _threadId;

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }
        private readonly bool _isValid;

        public AsyncUploadDownloadTask(SyncDirection Direction, Task<EventIdAndCompletionProcessor> Task, Guid ThreadId)
        {
            if (Task == null)
            {
                throw new NullReferenceException("Task cannot be null");
            }

            this._direction = Direction;
            this._task = Task;
            this._threadId = ThreadId;
            this._isValid = true;
        }
    }
}