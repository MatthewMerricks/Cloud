using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace CloudSdkSyncSample.EventMessageReceiver
{
    /// <summary>
    /// Helper object which can trigger a callback upon a data change from a binding (used in routing data changes to EventTriggers).
    /// </summary>
    public class BindingEvaluator : DependencyObject
    {
        /// <summary>
        /// Special singleton object intance used as binding default instead of null so that the data change callback can be triggered even for a null initial change.
        /// </summary>
        public sealed class Default
        {
            public static readonly Default Instance = new Default();
            private Default() { }
        }

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(object), typeof(BindingEvaluator), new PropertyMetadata(Default.Instance, ResultChanged));

        public static void ResultChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (((BindingEvaluator)sender).FireChanged != null)
            {
                ((BindingEvaluator)sender).FireChanged(e);
            }
        }

        public object Result
        {
            get
            {
                return (object)this.GetValue(ResultProperty);
            }
            set
            {
                this.SetValue(ResultProperty, value);
            }
        }

        private Action<DependencyPropertyChangedEventArgs> FireChanged;

        public BindingEvaluator(Binding toMonitor, Action<DependencyPropertyChangedEventArgs> fireChanged)
        {
            this.FireChanged = fireChanged;
            BindingOperations.SetBinding(this, ResultProperty, toMonitor);
        }

        public void RemoveBinding()
        {
            BindingOperations.ClearBinding(this, ResultProperty);
        }
    }
}