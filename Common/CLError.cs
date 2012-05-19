//
//  CLError.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Net;
using System.Windows;
using System.Collections.Generic;

namespace win_client.Common
{


    public class CLError
    {
        // Error codes
        public enum ErrorCodes
        {
            Exception,
        }

        // Error domains
        public static string ErrorDomain_Application = @"App";

        // errorInfo keys
        public static string ErrorInfo_Exception = @"Exception";

        public string errorDomain;
        public string errorDescriptionStringResourceKey;
        public int errorCode;
        public Dictionary<string, object> errorInfo;

        public CLError()
        {
            errorDomain = @"";
            errorDescriptionStringResourceKey = @"";
            errorCode = 0;
            errorInfo = new Dictionary<string, object>();
        }
    }
}
