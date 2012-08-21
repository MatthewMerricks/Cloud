//
// Push.cs
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
    public class Push
    {
        [DataMember(Name = CLDefinitions.CLMetadataCloudPath, IsRequired = false)]
        public string RelativeRootPath
        {
            get
            {
                if (!_relativeRootPathSet)
                {
                    _relativeRootPathSet = true;
                    _relativeRootPath = CLDefinitions.SyncBodyRelativeRootPath;
                }

                return _relativeRootPath;
            }
            set
            {
                _relativeRootPathSet = true;
                _relativeRootPath = value;
            }
        }
        private string _relativeRootPath = null;
        private bool _relativeRootPathSet = false;

        [DataMember(Name = CLDefinitions.CLSyncID, IsRequired = false)]
        public string LastSyncId { get; set; }
    }
}
