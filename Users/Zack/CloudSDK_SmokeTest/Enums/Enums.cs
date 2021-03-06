﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest
{
    public enum MainResult : int
    {
        Success = 0,
        UnknownError = 1,
        BadArguments = 2
    }
    
    [Flags]
    public enum FileManagerResponseCodes : int
    {
        Success = 0,
        UnknownError = 1,
        InitializeCredsError = 2,
        InitializeSynBoxError = 4,
        MD5HashingError = 8,
        InvalidTaskType = 16,
        InvalidResponseType = 32,
        ExpectedItemMatchFailure = 64,
    }

    public enum HttpPostReponseCodes
    { 
        None,
        Upload,
        Duplicate,
        Conflict,
        OK,
    }

    public enum FolderToUse
    { 
        None,
        Manual, 
        Active, 
        Active2,
    }
}