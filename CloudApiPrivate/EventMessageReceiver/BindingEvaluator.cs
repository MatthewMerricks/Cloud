//
//  BindingEvaluator.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace CloudApiPrivate.EventMessageReceiver
{
    public class BindingEvaluator : DependencyObject
    {
        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(object), typeof(BindingEvaluator), new PropertyMetadata(ResultChanged));

        public static void ResultChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (((BindingEvaluator)sender).FireChanged != null)
            {
                ((BindingEvaluator)sender).FireChanged(e);
            }
        }

        public object Result { get; set; }

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