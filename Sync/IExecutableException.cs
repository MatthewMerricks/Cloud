//
// IExecutableException.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sync
{
    /// <summary>
    /// Interface allowing an exception to be "executed" with an originally provided action and userstate
    /// </summary>
    public interface IExecutableException
    {
        /// <summary>
        /// Executes code to handle an exception which was provided as an Action on construction along with original userstate
        /// </summary>
        /// <param name="originalException">Passes through the original exception which required handling</param>
        /// <returns>Returns an exception that occurred handling the exception, if any</returns>
        Exception ExecuteException(AggregateException originalException);
    }
}