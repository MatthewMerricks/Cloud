//
// SetCountArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Properties for what a value was set to, meaning is according to which event passes these args
    /// </summary>
    public sealed class SetCountArgs : HandleableEventArgs
    {
        /// <summary>
        /// ID of the SyncBox
        /// </summary>
        public Nullable<long> SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private readonly Nullable<long> _syncBoxId;

        /// <summary>
        /// Unique ID for the device in the SyncBox
        /// </summary>
        public string DeviceId
        {
            get
            {
                return _deviceId;
            }
        }
        private readonly string _deviceId;

        public uint NewCount
        {
            get
            {
                return _newCount;
            }
        }
        private readonly uint _newCount;

        internal SetCountArgs(uint newCount, Nullable<long> SyncBoxId = null, string DeviceId = null)
        {
            this._newCount = newCount;
        }
    }
}