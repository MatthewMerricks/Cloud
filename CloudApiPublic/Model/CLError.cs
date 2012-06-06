//
//  CLError.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace CloudApiPublic.Model
{
    public class CLError
    {
        // Common error codes
        public enum ErrorCodes : int
        {
            Exception = 9999
        }

        // Error domains
        public const string ErrorDomain_Application = "Cloud";

        // errorInfo keys
        public const string ErrorInfo_Exception = "Exception";

        public string errorDomain;
        public string errorDescription;
        public int errorCode;
        public Dictionary<string, object> errorInfo;

        public CLError()
        {
            errorDomain = "";
            errorDescription = "";
            errorCode = 0;
            errorInfo = null;
        }

        public static implicit operator CLError(Exception ex)
        {
            return new CLError()
            {
                errorCode = (int)CLError.ErrorCodes.Exception,
                errorDescription = ex.Message,
                errorDomain = CLError.ErrorDomain_Application,
                errorInfo = new Dictionary<string, object>(1)
                {
                    { CLError.ErrorInfo_Exception, ex }
                }
            };
        }

        public void AddException(Exception ex, bool replaceErrorDescription = false)
        {
            this.errorInfo.Add(CLError.ErrorInfo_Exception + this.errorInfo.Count(currentPair => currentPair.Key.StartsWith(CLError.ErrorInfo_Exception)).ToString(),
                ex);
            if (replaceErrorDescription)
            {
                this.errorDescription = ex.Message;
            }
        }
    }
}
