//
//  CLException.cs
//  Cloud SDK Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cloud.Model
{
    /// <summary>
    /// Derived AggregateException class to contain Cloud error domain and code
    /// </summary> 
    public class CLException : AggregateException
    {
        /// <summary>
        /// Domain of the error
        /// </summary>
        public CLExceptionDomain Domain
        {
            get
            {
                return _domain;
            }
        }
        private readonly CLExceptionDomain _domain;

        /// <summary>
        /// Specific code of the error, grouped by domain
        /// </summary>
        public CLExceptionCode Code
        {
            get
            {
                return _code;
            }
        }
        private readonly CLExceptionCode _code;

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLException(
            CLExceptionCode code,
            string message,
            params Exception[] innerExceptions)
            : base(
                message,
                innerExceptions)
        {
            this._domain = (CLExceptionDomain)(((ulong)code) >> 32);
            this._code = code;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLException(
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> innerExceptions)
            : base(
                message,
                innerExceptions)
        {
            this._domain = (CLExceptionDomain)(((ulong)code) >> 32);
            this._code = code;
        }
    }
}