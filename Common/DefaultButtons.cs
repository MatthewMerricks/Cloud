//
//  DefaultButtons.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace win_client.Common
{
    public class DefaultButtonHub
    {
        ButtonAutomationPeer peer = null;

        private void Attach(DependencyObject source)
        {
            if(source is RadioButton)
            {
                RadioButton rb = source as RadioButton;
                rb.KeyUp += OnKeyUp;
            }
            else if(source is Button)
            {
                peer = new ButtonAutomationPeer(source as Button);
            }
            else if(source is TextBox)
            {
                TextBox tb = source as TextBox;
                tb.KeyUp += OnKeyUp;
            }
            else if(source is PasswordBox)
            {
                PasswordBox pb = source as PasswordBox;
                pb.KeyUp += OnKeyUp;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs arg)
        {
            if(arg.Key == Key.Enter)
                if(peer != null)
                    ((IInvokeProvider)peer).Invoke();
        }

        public static DefaultButtonHub GetDefaultHub(DependencyObject obj)
        {
            return (DefaultButtonHub)obj.GetValue(DefaultHubProperty);
        }

        public static void SetDefaultHub(DependencyObject obj, DefaultButtonHub value)
        {
            obj.SetValue(DefaultHubProperty, value);
        }

        // Using a DependencyProperty as the backing store for DefaultHub.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DefaultHubProperty =
                    DependencyProperty.RegisterAttached("DefaultHub", typeof(DefaultButtonHub), typeof(DefaultButtonHub), new PropertyMetadata(OnHubAttach));

        private static void OnHubAttach(DependencyObject source, DependencyPropertyChangedEventArgs prop)
        {
            DefaultButtonHub hub = prop.NewValue as DefaultButtonHub;
            hub.Attach(source);
        }
    }
}
