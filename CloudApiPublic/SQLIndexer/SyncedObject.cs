//
// SyncedObject.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cloud.Model;

namespace Cloud.SQLIndexer
{
    internal class SyncedObject
    {
        public string ServerLinkedPath { get; set; }
        public FileMetadata Metadata { get; set; }
    }
}