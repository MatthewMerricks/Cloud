//
// CLFileItemCompletionCallback.cs
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
    /// Completion routine for an individual CLFileItem operation. Either completedItem will be null or error will be null.
    /// </summary>
    /// <param name="itemIndex">Zero-based index of the current item in its original input set</param>
    /// <param name="completedItem">Completed item or null if there was an error</param>
    /// <param name="error">An error which occurred for the current item, if any</param>
    /// <param name="userState">The state provided by the user</param>
    public delegate void CLFileItemCompletionCallback(int itemIndex, CLFileItem completedItem, CLError error, object userState);
}