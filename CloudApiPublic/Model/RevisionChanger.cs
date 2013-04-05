//
// RevisionChanger.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    internal class RevisionChanger
    {
        public event EventHandler<RevisionAndOtherDataEventArgs> RevisionChanged;

        public void FireRevisionChanged(FileMetadata toFire)
        {
            lock (this.RevisionChangeLocker)
            {
                if (RevisionChanged != null)
                {
                    RevisionChanged(toFire, new RevisionAndOtherDataEventArgs(toFire.Revision, toFire.ServerUid));
                }
            }
        }

        public readonly object RevisionChangeLocker = new object();
    }

    internal sealed class RevisionAndOtherDataEventArgs : EventArgs
    {
        public string Revision
        {
            get
            {
                return _revision;
            }
        }
        private readonly string _revision;

        public string ServerUid
        {
            get
            {
                return _serverUid;
            }
        }
        private readonly string _serverUid;

        public RevisionAndOtherDataEventArgs(string revision, string serverUid)
        {
            this._revision = revision;
            this._serverUid = serverUid;
        }
    }
}