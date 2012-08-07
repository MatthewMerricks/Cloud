//
//  DialogFolderSelectionSimpleView.xaml.cs
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
    public partial class DialogFolderSelectionSimpleView : Window, IModalWindow
    {
        public DialogFolderSelectionSimpleView()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(DialogFolderSelectionSimpleView_Loaded);
        }

        //TODO: FocusedElement is a ChildWindow DependencyProperty, properly registered, but for some reason some of
        // the dependency properties are not firing.  FocusedElement is one of them.  Setting this property
        // via the code-behind works however, so we do it here.
        void DialogFolderSelectionSimpleView_Loaded(object sender, RoutedEventArgs e)
        {
            btnOK.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
