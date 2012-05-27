//
//  CLApiError.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;

namespace CloudApi
{
    public class CLApiError
    {
        // Common error codes
        public enum ErrorCodes
        {
            OK = 0,
            Exception = 9999,
        }

        // Error domains
        public const string ErrorDomain_Application = "Cloud";

        // errorInfo keys
        public const string ErrorInfo_Exception = "Exception";

        public string errorDomain;
        public string errorDescription;
        public int errorCode;
        public Dictionary<string, object> errorInfo;

        public CLApiError()
        {
            errorDomain = "";
            errorDescription = "";
            errorCode = 0;
            errorInfo = null;
        }
    }
}
