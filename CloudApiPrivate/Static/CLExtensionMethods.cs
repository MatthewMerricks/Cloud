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
using Cloud.Support;
using CloudApiPrivate.Common;
using Cloud.Model;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Reflection;
using System.Linq;

namespace CloudApiPrivate.Static
{

    public static class CLExtensionMethods
    {
        /// <summary> 
        /// Extend string.  Return equivalent of Objective C lastPathComponent.  
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <returns>string.  The last path component.</returns> 
        /// Call like this:
        /// myStringLastPathComponent = sourceString.LastPathComponent;
        public static string LastPathComponent(this string source)
        {
            return Path.GetFileName(source);
        }

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
            for(int i = 0; i < VisualTreeHelper.GetChildrenCount((DependencyObject)element); i++)
            {
                object child = VisualTreeHelper.GetChild((DependencyObject)element, i);
                ForceValidation(child);
            }

            BindingExpression bindingExpression = null;

            string uiElementType = element.GetType().ToString();
            switch (uiElementType)
            {
                case "System.Windows.Controls.TextBox":
                    bindingExpression = ((TextBox)element).GetBindingExpression(TextBox.TextProperty);
                    break;

                case "CloudApiPrivate.Common.SecurePasswordBox":
                    bindingExpression = ((SecurePasswordBox)element).GetBindingExpression(SecurePasswordBox.TextProperty);
                    break;

                case "System.Windows.Controls.RadioButton":
                    bindingExpression = ((RadioButton)element).GetBindingExpression(RadioButton.IsCheckedProperty);
                    break;
            }

            if (bindingExpression == null || bindingExpression.ParentBinding == null)
            {
                return;
            }
            if (!bindingExpression.ParentBinding.ValidatesOnDataErrors)
            {
                return;
            }
            bindingExpression.UpdateSource();
        }

        /// <summary> 
        /// Extend DependencyObject: Finds a Child of a given item in the visual tree.  
        /// </summary> 
        /// <param name="parent">A direct parent of the queried item.</param> 
        /// <typeparam name="T">The type of the queried item.  This must be a DependencyObject.</typeparam> 
        /// <param name="childName">x:Name or Name of child. </param> 
        /// <returns>The first parent item that matches the submitted type parameter.  
        /// If not matching item can be found,  
        /// a null parent is being returned.</returns> 
        /// 
        /// Call like this:
        /// TextBox foundTextBox =  UIHelper.FindChild<TextBox>(Application.Current.MainWindow, "myTextBoxName"); 
        /// 
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
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

        /// <summary> 
        /// Extend Dictionary<T,S>: Adds one dictionary to another.  
        /// </summary> 
        /// <param name="source">The dictionary that will be added to.</param> 
        /// <param name="collection">The dictionary to be added. </param> 
        /// <returns>void</returns> 
        /// Call like this:
        /// animals.AddRange(newanimals)
        /// 
        public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else
                {
                    // Duplicate key detected.
                    throw new Exception("Duplicate key <" + item.Key.ToString() + "> detected.");
                }
            }
        }

        /// <summary> 
        /// Extend string.  Return equivalent of Objective C stringByDeletingLastPathComponent.  
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <returns>string.  The string without the last path component.</returns> 
        /// Call like this:
        /// myStringWithoutLastPathComponent = sourceString.StringByDeletingLastPathComponent;
        public static string StringByDeletingLastPathComponent(this string source)
        {
            return Path.GetFullPath(source);
        }

        /// <summary> 
        /// Extend string.  Return equivalent of Objective C pathExtension.  
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <returns>string.  The extension.</returns> 
        /// Call like this:
        /// myStringExtension = sourceString.PathExtension();
        public static string PathExtension(this string source)
        {
            return Path.GetExtension(source);
        }

        /// <summary> 
        /// Extend string.  Return equivalent of Objective C pathComponents.
        /// This is the list of path components in the directory item, or the
        /// parent directory of the file item.
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <returns>string[].  The path components array.</returns> 
        /// Call like this:
        /// string[] myPathComponents = sourceString.PathComponents();
        public static string[] PathComponents(this string source)
        {
            var separators = new char[] {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            };

            return source.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary> 
        /// Extend string.  Return equivalent of Objective C SubstringToIndex.
        /// This is the substring at the beginning of the string up to, but
        /// not including, the indexed character.
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <returns>string. The resulting substring.</returns> 
        /// Call like this:
        /// string mySubstring = sourceString.SubstringToIndex();
        public static string SubstringToIndex(this string source, Int32 toIndex)
        {
            return source.Substring(0, toIndex);
        }

        /// <summary> 
        /// 
        /// Extend string.  Return equivalent of Objective C RangeOfString.
        /// This is the CLRange (Location and Position) of the search string in the source string.
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <param name="search">The string to search for.</param> 
        /// <returns>CLRange. The resulting range.</returns> 
        /// Call like this:
        /// CLRange myRange = sourceString.RangeOfString("search string");
        public static CLRange RangeOfString(this string source, string search)
        {
            CLRange range = new CLRange();
            range.Location = source.IndexOf(search);
            range.Length = search.Length;
            
            return range;
        }
        public class CLRange
        {
            public int Location { get; set; }
            public int Length { get; set; }
        }

        /// <summary> 
        /// Extend string.  Return equivalent of Objective C StringByReplacingCharactersInRange:withString.
        /// </summary> 
        /// <param name="source">The source string.</param> 
        /// <param name="range">The CLRange restriction.</param> 
        /// <param name="withString">The string to substitute.</param> 
        /// <returns>string. The resulting string.</returns> 
        /// Call like this:
        /// string myOutputString = sourceString.StringByReplacingCharactersInRange((CLRange) range, "string to substitute");
        public static string StringByReplacingCharactersInRange(this string source, CLRange range, string withString)
        {
            source.Remove(range.Location, range.Length);
            source.Insert(range.Location, withString);
            return source;
        }

        /// <summary> 
        /// Extend Stream.  Read a stream into a structure.
        /// </summary> 
        /// <param name="stream">The source stream.</param> 
        /// <returns>T. The resulting filled structure.</returns> 
        /// Call like this:
        /// MemoryStream ms = new MemoryStream(data);
        /// StructType filledStructure = ReadStruct<StructType>(ms)
        /// 
        /// [StructLayout(LayoutKind.Sequential, CharSet.Ansi, Pack = 1)]
        /// internal struct StructType
        /// {
        ///     [MarshaAs(UnmanagedType.ByValArray, SizeConst = 16)]
        ///     public byte[] stuff;
        ///     etc...
        /// }

        public static T ReadStruct<T>(this Stream stream) where T : struct    
        {
            var sz = Marshal.SizeOf(typeof(T));
            var buffer = new byte[sz];
            stream.Read(buffer, 0, sz);
            var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var structure = (T)Marshal.PtrToStructure(
                pinnedBuffer.AddrOfPinnedObject(), typeof(T));
            pinnedBuffer.Free();
            return structure;
        }

    }
}
