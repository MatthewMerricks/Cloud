//
// Enums.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic
{
    public enum FileChangeQueryStatus
    {
        Success,
        ErrorMultipleResults,
        ErrorNotFound,
        ErrorUnknown
    }
}