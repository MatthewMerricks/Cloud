﻿//
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
        public class Default
        {
            public static Default Instance = new Default();
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