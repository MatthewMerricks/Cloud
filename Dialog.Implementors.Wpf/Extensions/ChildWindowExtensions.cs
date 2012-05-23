using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Dialog.Abstractions.Wpf.Intefaces;
using System.Windows;

namespace Xceed.Wpf.Toolkit
{
    public static class ChildWindowExtensions
    {
        private static Dictionary<IModalWindow, Grid> childWindowContainers = new Dictionary<IModalWindow, Grid>();

        public static void Show(this IModalWindow toExtend, Grid container)
        {
            UIElement extendElement = toExtend as UIElement;

            if (extendElement != null)
            {
                lock (childWindowContainers)
                {
                    if (!childWindowContainers.ContainsKey(toExtend))
                    {
                        toExtend.Closed += toExtend_Closed;
                        childWindowContainers.Add(toExtend, container);
                        container.Children.Add(extendElement);
                        container.ApplyTemplate();
                    }
                }
                toExtend.Show();
            }
        }

        private static void toExtend_Closed(object sender, EventArgs e)
        {
            IModalWindow castSender = (IModalWindow)sender;
            UIElement senderElement = sender as UIElement;

            if (senderElement != null)
            {
                lock (childWindowContainers)
                {
                    if (childWindowContainers.ContainsKey(castSender))
                    {
                        Grid container = childWindowContainers[castSender];
                        if (container.Children.Contains(senderElement))
                            container.Children.Remove(senderElement);
                        childWindowContainers.Remove(castSender);
                    }
                }
            }
        }
    }
}
