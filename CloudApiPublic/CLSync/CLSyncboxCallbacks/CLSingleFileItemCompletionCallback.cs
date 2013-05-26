//
// CLSingleFileItemCompletionCallback.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.CLSync
{
    /// <summary>
    /// Completion routine for a single CLFileItem operation.
    /// </summary>
    /// <param name="completedItem">Completed item.</param>
    /// <param name="userState">The state provided by the user</param>
    public delegate void CLSingleFileItemCompletionCallback(CLFileItem completedItem, object userState);
}