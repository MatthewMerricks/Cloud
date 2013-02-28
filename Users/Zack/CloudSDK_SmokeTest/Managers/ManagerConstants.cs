using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public static  class ManagerConstants
    {
        public const int TimeOutMilliseconds = 10000;

        public static string[] DefaultFolderNames = new string[] { "/Documents/", "/Videos/", "/Pictures/" };

        public const int WaitMillisecondsBeforeCompare = 30000; 

        public static  class RequestTypes
        {
            public const string PostFileChange = "PostFileChange";
            public const string RestCreateFile = "CreateFile";
            public const string RestCreaateFolder = "CreateFolder";
        }
    }
}
