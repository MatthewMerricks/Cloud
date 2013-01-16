//
// IncrementCountArgs.cs
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
    /// Properties for how much a value was incremented, meaning is according to which event passes these args
    /// </summary>
    public sealed class IncrementCountArgs : HandleableEventArgs
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

        public uint IncrementAmount
        {
            get
            {
                return _incrementAmount;
            }
        }
        private readonly uint _incrementAmount;

        internal IncrementCountArgs(uint incrementAmount = 1, Nullable<long> SyncBoxId = null, string DeviceId = null)
        {
            this._incrementAmount = incrementAmount;
        }
    }
}