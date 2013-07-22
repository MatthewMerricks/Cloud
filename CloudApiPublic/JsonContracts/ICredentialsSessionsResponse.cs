//
// ICredentialsSessionsResponse.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cloud.JsonContracts
{
    [Obfuscation(Exclude = true)]
    internal interface ICredentialsSessionsResponse
    {
        string Status { get; }

        string Message { get; }

        Session[] Sessions { get; }
    }
}