//
//  GrowlScrollContentPresenterMouseLeftDownBehavior.cs
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
using System.Windows.Input;
using System.Windows.Interactivity;

namespace win_client.Growl
{
    public sealed class GrowlScrollContentPresenterMouseLeftDownBehavior : Behavior<FrameworkElement>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += growlPresenter_MouseLeftButtonDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= growlPresenter_MouseLeftButtonDown;
        }

        private void growlPresenter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GrowlScrollViewer growlScroller = AssociatedObject.TemplatedParent as GrowlScrollViewer;
            if (growlScroller != null
                && growlScroller.MouseLeftButtonDownCommand != null)
            {
                growlScroller.MouseLeftButtonDownCommand.Execute(growlScroller.GetValue(GrowlScrollViewer.MouseLeftButtonDownCommandParameterProperty));

                e.Handled = true;
            }
        }
    }
}