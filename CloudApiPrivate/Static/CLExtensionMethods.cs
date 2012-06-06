//
//  CLExtensionMethods.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CloudApiPublic.Support;
using CloudApiPrivate.Common;


namespace CloudApiPrivate.Static
{

    public static class CLExtensionMethods
    {
        /// <summary>
        /// Generic object deep copy.
        /// </summary>
        public static T DeepCopy<T>(this T oSource)
        {

            T oClone;
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                dcs.WriteObject(ms, oSource);
                ms.Position = 0;
                oClone = (T)dcs.ReadObject(ms);
            }
            return oClone;
        }

        /// <summary>
        /// Validate the whole UI tree.
        /// </summary>
        public static void ForceValidation(object element)
        {
            var trace = CLTrace.Instance;
            trace.writeToLog(0, "ForceValidation: Entry.  element: {0}.", element.ToString());
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount((DependencyObject)element); i++)
            {
                object child = VisualTreeHelper.GetChild((DependencyObject)element, i);
                trace.writeToLog(9, "ForceValidation: Found child: {0}. index: {1}.  Recurse.", child.ToString(), i);
                ForceValidation(child);
            }

            BindingExpression bindingExpression = null;

            string uiElementType = element.GetType().ToString();
            trace.writeToLog(9, "ForceValidation: Check this element. uiElementType: {0}.", uiElementType);
            switch (uiElementType)
            {
                case "System.Windows.Controls.TextBox":
                    trace.writeToLog(9, "ForceValidation: This is a TextBox.");
                    bindingExpression = ((TextBox)element).GetBindingExpression(TextBox.TextProperty);
                    break;

#if SILVERLIGHT 
                case "System.Windows.Controls.PasswordBox":
                    bindingExpression = ((PasswordBox)element).GetBindingExpression(PasswordBox.PasswordProperty);
                    break;
#else
                case "win_client.Common.SecurePasswordBox":
                    trace.writeToLog(9, "ForceValidation: This is a SecurePasswordBox.");
                    bindingExpression = ((SecurePasswordBox)element).GetBindingExpression(SecurePasswordBox.TextProperty);
                    break;
#endif

                case "System.Windows.Controls.RadioButton":
                    trace.writeToLog(9, "ForceValidation: This is a RadioButton.");
                    bindingExpression = ((RadioButton)element).GetBindingExpression(RadioButton.IsCheckedProperty);
                    break;
            }

            if (bindingExpression == null || bindingExpression.ParentBinding == null)
            {
                trace.writeToLog(9, "ForceValidation: Binding or ParentBinding is null.  Return.");
                return;
            }
#if SILVERLIGHT 
            if(!bindingExpression.ParentBinding.ValidatesOnNotifyDataErrors) return;
#else
            if (!bindingExpression.ParentBinding.ValidatesOnDataErrors)
            {
                trace.writeToLog(9, "ForceValidation: Parent does not validate on data errors.  Return.");
                return;
            }
#endif
            trace.writeToLog(9, "ForceValidation: Update the source for this binding expression.");
            bindingExpression.UpdateSource();
            trace.writeToLog(9, "ForceValidation: Return.");
        }

        /// <summary>
        /// Extend Dispatcher.*
        /// Delayed invocation on the UI thread without arguments.
        /// </summary>
        public static void DelayedInvoke(this Dispatcher dispatcher, TimeSpan delay, Action action)
        {
            Thread thread = new Thread(DoDelayedInvokeByAction);
            thread.Start(new Tuple<Dispatcher, TimeSpan, Action>(dispatcher, delay, action));
        }

        ///<summary>
        ///Private delayed invocation by action.
        ///</summary>
        private static void DoDelayedInvokeByAction(object parameter)
        {
            Tuple<Dispatcher, TimeSpan, Action> parameterData = (Tuple<Dispatcher, TimeSpan, Action>)parameter;

            Thread.Sleep(parameterData.Item2);

            parameterData.Item1.BeginInvoke(parameterData.Item3);
        }

        /// <summary>
        /// Delayed invocation on the UI thread with arguments.
        /// </summary>
        public static void DelayedInvoke(this Dispatcher dispatcher, TimeSpan delay, System.Delegate d, params object[] args)
        {
            Thread thread = new Thread(DoDelayedInvokeByDelegate);
            thread.Start(new Tuple<Dispatcher, TimeSpan, System.Delegate, object[]>(dispatcher, delay, d, args));
        }

        /// <summary>
        /// Private delayed invocation by delegate.
        /// </summary>
        private static void DoDelayedInvokeByDelegate(object parameter)
        {
            Tuple<Dispatcher, TimeSpan, System.Delegate, object[]> parameterData = (Tuple<Dispatcher, TimeSpan, System.Delegate, object[]>)parameter;

            Thread.Sleep(parameterData.Item2);

            parameterData.Item1.BeginInvoke(parameterData.Item3, parameterData.Item4);
        }

        /// <summary> 
        /// Extend DependencyObject: Finds a Child of a given item in the visual tree.  
        /// </summary> 
        /// <param name="parent">A direct parent of the queried item.</param> 
        /// <typeparam name="T">The type of the queried item.</typeparam> 
        /// <param name="childName">x:Name or Name of child. </param> 
        /// <returns>The first parent item that matches the submitted type parameter.  
        /// If not matching item can be found,  
        /// a null parent is being returned.</returns> 
        /// 
        /// Call like this:
        /// TextBox foundTextBox =  UIHelper.FindChild<TextBox>(Application.Current.MainWindow, "myTextBoxName"); 
        /// 
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid.  
            if(parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for(int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child 
                T childType = child as T;
                if(childType == null)
                {
                    // recursively drill down the tree 
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.  
                    if(foundChild != null) break;
                }
                else if(!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search 
                    if(frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name 
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found. 
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        } 

    }

}
