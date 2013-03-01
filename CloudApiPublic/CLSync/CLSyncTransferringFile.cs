//
// CLSyncTransferringFile.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud
{
    /// <summary>
    /// Type for both a transferring upload (Direction is To) as well as a transferring download (Direction is From); contains status properties
    /// </summary>
    public sealed class CLSyncTransferringFile
    {
        public long EventId
        {
            get
            {
                return _eventId;
            }
        }
        private readonly long _eventId;

        public SyncDirection Direction
        {
            get
            {
                return _direction;
            }
        }
        private readonly SyncDirection _direction;

        public string RelativePath
        {
            get
            {
                return _relativePath;
            }
        }
        private readonly string _relativePath;

        public long ByteProgress
        {
            get
            {
                return _byteProgress;
            }
        }
        private readonly long _byteProgress;

        public long TotalByteSize
        {
            get
            {
                return _totalByteSize;
            }
        }
        private readonly long _totalByteSize;

        internal CLSyncTransferringFile(long EventId,
            SyncDirection Direction,
            string RelativePath,
            long ByteProgress,
            long TotalByteSize)
        {
            this._eventId = EventId;
            this._direction = Direction;
            this._relativePath = RelativePath;
            this._byteProgress = ByteProgress;
            this._totalByteSize = TotalByteSize;
        }
    }
}