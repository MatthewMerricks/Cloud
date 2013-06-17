//
//  CLHttpException.cs
//  Cloud SDK Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.

using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace Cloud
{
    /// <summary>
    /// Derived CLException class to contain more specific information relating to Http errors, if provided
    /// </summary>
    public sealed class CLHttpException : CLException
    {
        /// <summary>
        /// Http status code returned from the server, if received
        /// </summary>
        public Nullable<HttpStatusCode> HttpStatus
        {
            get
            {
                return _status;
            }
        }
        private readonly Nullable<HttpStatusCode> _status;

        /// <summary>
        /// Response from the server in text format, if received
        /// </summary>
        public string HttpResponse
        {
            get
            {
                return _response;
            }
        }
        private readonly string _response;

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLHttpException(
            Nullable<HttpStatusCode> status,
            string response,
            CLExceptionCode code,
            string message,
            params Exception[] original)
            : base(code, message, original)
        {
            this._status = status;
            this._response = response;
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        internal CLHttpException(
            Nullable<HttpStatusCode> Status,
            string Response,
            CLExceptionCode code,
            string message,
            IEnumerable<Exception> original)
            : base(code, message, original)
        {
            this._status = Status;
            this._response = Response;
        }
    }
}