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
using CloudApiPublic.Model;

namespace SQLIndexer
{
    public class SyncedObject
    {
        public string ServerLinkedPath { get; set; }
        public FileMetadata Metadata { get; set; }
    }
}