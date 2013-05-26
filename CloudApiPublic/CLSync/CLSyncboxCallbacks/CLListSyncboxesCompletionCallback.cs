//
// CLListSyncboxesCompletionCallback.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.CLSync
{
    /// <summary>
    /// Completion routine for the CLSyncbox.ListSyncboxes method.
    /// </summary>
    /// <param name="syncbox">The syncboxes returned.</param>
    /// <param name="userState">The state provided by the user.</param>
    public delegate void CLListSyncboxesCompletionCallback(CLSyncbox[] syncboxes, object userState);
}