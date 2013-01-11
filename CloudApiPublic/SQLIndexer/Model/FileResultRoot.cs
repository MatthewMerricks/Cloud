//
// FileResultRoot.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.SQLIndexer.Model
{
    internal sealed class FileResultRoot : IFileResultParent
    {
        private string RootName;

        public string FullName
        {
            get
            {
                return RootName;
            }
        }

        public FileResultRoot(string rootName)
        {
            this.RootName = rootName;
        }
    }
}