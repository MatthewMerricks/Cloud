using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                IEnumerable<Attribute> methodCustomAttributes;
                return (((methodCustomAttributes = CLSyncboxMethodInfo.GetCustomAttributes(typeof(EditorBrowsableAttribute))) != null
                        && methodCustomAttributes.Any(methodCustomAttribute => ((EditorBrowsableAttribute)methodCustomAttribute).State == EditorBrowsableState.Never))
                    ? "(hidden) "
                    : string.Empty) + CLSyncboxMethodInfo.Name;
            }
        }

        public CLSyncboxAction(MethodInfo CLSyncboxMethodInfo)
        {
            Debug.Assert(CLSyncboxMethodInfo != null, "CLSyncboxMethodInfo cannot be null");

            this.CLSyncboxMethodInfo = CLSyncboxMethodInfo;
        }
    }
}