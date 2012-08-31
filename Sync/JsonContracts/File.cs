﻿//
// File.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using CloudApiPublic.Model;

namespace Sync.JsonContracts
{
    [DataContract]
    public class File
    {
        [DataMember(Name = CLDefinitions.CLMetadataFile, IsRequired = false)]
        public Metadata Metadata { get; set; }
    }
}