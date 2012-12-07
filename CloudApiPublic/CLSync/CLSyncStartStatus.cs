using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic
{
    public enum CLSyncStartStatus : byte
    {
        ErrorUnknown,
        ErrorLongRootPath,
        ErrorBadRootPath,
        Successful
    }
}