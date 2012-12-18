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
    public sealed class PushSettings : IHttpSettings
    {
        public string Udid
        {
            get
            {
                return _udid;
            }
        }
        private string _udid;

        public string ApplicationKey
        {
            get
            {
                return _applicationKey;
            }
        }
        private string _applicationKey;

        public string ApplicationSecret
        {
            get
            {
                return _applicationSecret;
            }
        }
        private string _applicationSecret;

        public string SyncBoxId
        {
            get
            {
                return _syncBoxId;
            }
        }
        private string _syncBoxId;

        public PushSettings(
            string udid,
            string applicationKey,
            string applicationSecret,
            string syncBoxId
            )
        {
            this._udid = udid;
            this._applicationKey = applicationKey;
            this._applicationSecret = applicationSecret;
            this._syncBoxId = syncBoxId;
        }
    }
}