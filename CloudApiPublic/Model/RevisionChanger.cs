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
        public event EventHandler<ResolveEventArgs> RevisionChanged;

        public void FireRevisionChanged(FileMetadata toFire)
        {
            lock (this.RevisionChangeLocker)
            {
                if (RevisionChanged != null)
                {
                    RevisionChanged(toFire, new ResolveEventArgs(toFire.Revision));
                }
            }
        }

        public readonly object RevisionChangeLocker = new object();
    }

    public class put event args here for revision plus server id
}