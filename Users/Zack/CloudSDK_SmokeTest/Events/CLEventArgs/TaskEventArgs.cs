using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class TaskEventArgs : CLEventArgs
    {
        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }
        public SmokeTask CurrentTask { get; set; }
    }
}
