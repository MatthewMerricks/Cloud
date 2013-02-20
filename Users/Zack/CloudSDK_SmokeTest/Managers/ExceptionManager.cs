using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Managers
{
    public sealed class ExceptionManager
    {
        public static Exception ReturnException(string opperationName, string statusString)
        {
            return new Exception(string.Format("The Status Returned From {0} is {1}", opperationName, statusString));
        }
    }
}
