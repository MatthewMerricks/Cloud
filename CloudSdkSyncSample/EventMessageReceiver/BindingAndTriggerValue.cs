using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace SampleLiveSync.EventMessageReceiver
{
    /// <summary>
    /// Holder for a binding and a value to compare the bound property value (used in routing data changes to EventTriggers). Does not contain any code to evaluate the binding nor run the comparison.
    /// </summary>
    public class BindingAndTriggerValue
    {
        public Binding Binding { get; set; }
        public object Value { get; set; }
    }
}