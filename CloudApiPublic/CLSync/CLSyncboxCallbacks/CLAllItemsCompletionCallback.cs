//
// CLAllItemsCompletionCallback.cs
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
    /// Completion routine for an "all items" CLFileItem query operation.
    /// </summary>
    /// <param name="itemsx">All items returned.</param>
    /// <param name="totalItemCount">The number of items returned.</param>
    /// <param name="userState">The state provided by the user.</param>
    public delegate void CLAllItemsCompletionCallback(CLFileItem[] items, long totalItemCount, object userState);
}