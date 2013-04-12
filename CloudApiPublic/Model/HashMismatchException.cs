//
// HashMismatchException.cs
// Cloud Windows
//
// Created By GeorgeS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    /// <summary>
    /// Exception to mark a hash mismatch in the file stream because of intermediate writes to the file 
    /// </summary>
    internal sealed class HashMismatchException : Exception
    {
        public HashMismatchException(string message)
            : base(message)
        {
        }
    }
}