//
// EventMessageArgs.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    /// <summary>
    /// Contains event message properties
    /// </summary>
    public sealed class EventMessageArgs : HandleableEventArgs
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

        /// <summary>
        /// The actual message
        /// </summary>
        public string Message
        {
            get
            {
                return _message;
            }
        }
        private readonly string _message;

        /// <summary>
        /// Whether the message is for an error
        /// </summary>
        public bool IsError
        {
            get
            {
                return _isError;
            }
        }
        private readonly bool _isError;

        /// <summary>
        /// The importance of this message from 1 to 9 (as an enum)
        /// </summary>
        public EventMessageLevel Level
        {
            get
            {
                return _level;
            }
        }
        private readonly EventMessageLevel _level;

        internal EventMessageArgs(string Message, EventMessageLevel Level = EventMessageLevel.Minor, bool IsError = false, Nullable<long> SyncBoxId = null, string DeviceId = null)
        {
            this._message = Message;
            this._level = Level;
            this._isError = IsError;
            this._syncBoxId = SyncBoxId;
            this._deviceId = DeviceId;
        }
    }
}