//
//  GrowlScrollViewer.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace win_client.Growl
{
    public class GrowlScrollViewer : ScrollViewer
    {
        public static DependencyProperty MouseLeftButtonDownCommandProperty = DependencyProperty.Register("MouseLeftButtonDownCommand",
            typeof(ICommand),
            typeof(GrowlScrollViewer),
            new PropertyMetadata(new RelayCommand<GrowlScrollViewer>(scroller => scroller.RaiseEvent(new RoutedEventArgs(GrowlScrollViewer.MouseLeftButtonDownEvent)))));

        public ICommand MouseLeftButtonDownCommand
        {
            get
            {
                return (ICommand)this.GetValue(MouseLeftButtonDownCommandProperty);
            }
            set
            {
                this.SetValue(MouseLeftButtonDownCommandProperty, value);
            }
        }

        public static DependencyProperty MouseLeftButtonDownCommandParameterProperty = DependencyProperty.Register("MouseLeftButtonDownCommandParameter",
            typeof(object),
            typeof(GrowlScrollViewer));

        public object MouseLeftButtonDownCommandParameter
        {
            get
            {
                return this.GetValue(MouseLeftButtonDownCommandParameterProperty);
            }
            set
            {
                this.SetValue(MouseLeftButtonDownCommandParameterProperty, value);
            }
        }
    }
}