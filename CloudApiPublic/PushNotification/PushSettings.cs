//
// PushSettings.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using CloudApiPublic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.PushNotification
{
    /// <summary>
    /// Simple implementation of IPushSettings
    /// </summary>
    public sealed class PushSettings : IPushSettings
    {
        public string Udid
        {
            get
            {
                return _udid;
            }
        }
        private string _udid;

        public string Uuid
        {
            get
            {
                return _uuid;
            }
        }
        private string _uuid;

        public string Akey
        {
            get
            {
                return _akey;
            }
        }
        private string _akey;

        public PushSettings(
                    string udid,
                    string uuid,
                    string akey)
        {
            this._udid = udid;
            this._uuid = uuid;
            this._akey = akey;
        }
    }
}