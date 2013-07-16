using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Models.CLSyncboxActions
{
    public sealed class CLSyncboxAction
    {
        private readonly MethodInfo CLSyncboxMethodInfo;

        public string Name
        {
            get
            {
                return CLSyncboxMethodInfo.Name;
            }
        }

        public CLSyncboxAction(MethodInfo CLSyncboxMethodInfo)
        {
            Debug.Assert(CLSyncboxMethodInfo != null, "CLSyncboxMethodInfo cannot be null");

            this.CLSyncboxMethodInfo = CLSyncboxMethodInfo;
        }
    }
}