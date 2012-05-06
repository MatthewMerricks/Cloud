//
//  ExtensionMethods.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;

namespace win_client.Common
{


    public static class ExtensionMethods
    {
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
        public static void ForceValidation(UIElement element)
        {
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                UIElement child = (UIElement)VisualTreeHelper.GetChild(element, i);
                ForceValidation(child);
            }

            BindingExpression bindingExpression = null;

            string uiElementType = element.GetType().ToString();
            switch(uiElementType)
            {
                case "System.Windows.Controls.TextBox":
                    bindingExpression = ((TextBox)element).GetBindingExpression(TextBox.TextProperty);
                    break;

                case "System.Windows.Controls.PasswordBox":
                    bindingExpression = ((PasswordBox)element).GetBindingExpression(PasswordBox.PasswordProperty);
                    break;

                case "System.Windows.Controls.RadioButton":
                    bindingExpression = ((RadioButton)element).GetBindingExpression(RadioButton.IsCheckedProperty);
                    break;
            }

            if(bindingExpression == null || bindingExpression.ParentBinding == null) return;
            if(!bindingExpression.ParentBinding.ValidatesOnNotifyDataErrors) return;

            bindingExpression.UpdateSource();
        }
    }
}
