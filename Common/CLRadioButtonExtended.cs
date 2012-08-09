//
//  CLRadioButtonExtended.cs
//  Cloud Windows
//  From: http://pstaev.blogspot.com/2008/10/binding-ischecked-property-of.html
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace win_client.Common
{
    public class CLRadioButtonExtended : RadioButton
    {
        static bool m_bIsChanging = false;

        public CLRadioButtonExtended()
        {
            this.Checked += new RoutedEventHandler(RadioButtonExtended_Checked);
            this.Unchecked += new RoutedEventHandler(RadioButtonExtended_Unchecked);
        }

        void RadioButtonExtended_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsChanging)
                this.IsCheckedReal = false;
        }

        void RadioButtonExtended_Checked(object sender, RoutedEventArgs e)
        {
            if (!m_bIsChanging)
                this.IsCheckedReal = true;
        }

        public bool? IsCheckedReal
        {
            get { return (bool?)GetValue(IsCheckedRealProperty); }
            set
            {
                SetValue(IsCheckedRealProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for IsCheckedReal. This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCheckedRealProperty =
        DependencyProperty.Register("IsCheckedReal", typeof(bool?), typeof(CLRadioButtonExtended),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Journal |
        FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
        IsCheckedRealChanged));

        public static void IsCheckedRealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            m_bIsChanging = true;
            ((CLRadioButtonExtended)d).IsChecked = (bool)e.NewValue;
            m_bIsChanging = false;
        }
    }
}
