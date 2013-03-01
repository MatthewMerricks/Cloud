using CloudApiPublic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public class ModifyPlanEventArgs : TaskEventArgs
    {
        public long PlanID { get; set; }
    }
}
