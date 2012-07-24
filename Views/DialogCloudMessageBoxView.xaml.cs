//
//  PageCreateNewAccount.xaml.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Data;
using win_client.Common;
using win_client.ViewModels;
using Dialog.Abstractions.Wpf.Intefaces;
using Xceed.Wpf.Toolkit;

namespace win_client.Views
{
    public partial class DialogCloudMessageBoxView : ChildWindow, IModalWindow
    {
        public DialogCloudMessageBoxView()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(DialogCloudMessageBoxView_Loaded);
        }

        // Button clicks set the DialogResult.
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        //TODO: FocusedElement is a ChildWindow DependencyProperty, properly registered, but for some reason some of
        // the dependency properties are not firing.  FocusedElement is one of them.  Setting this property
        // via the code-behind works however, so we do it here.
        void DialogCloudMessageBoxView_Loaded(object sender, RoutedEventArgs e)
        {
            FocusedElement = btnOK;
        }

        // The following added to allow the Title, Width and Height= fields of this ChildWindow to use a binding back to the ViewModel:
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
              typeof(string),
              typeof(ChildWindow),
              new PropertyMetadata(""));

        public new string Width
        {
            get { return (string)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }
        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register("Width",
              typeof(string),
              typeof(ChildWindow),
              new PropertyMetadata(""));

        public new string Height
        {
            get { return (string)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }
        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register("Height",
              typeof(string),
              typeof(ChildWindow),
              new PropertyMetadata(""));
    }
}
