//
// Helpers.cs
// Cloud SDK Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Security.Principal;

namespace Cloud.Static
{
    /// <summary>
    /// Class containing commonly usable static helper methods
    /// </summary>
    public static class Helpers
    {

        /// <summary>
        /// Creates a default instance of a provided type for use with populating out parameters when exceptions are thrown
        /// </summary>
        /// <param name="toDefault">Type to return</param>
        /// <returns>Default value of provided type</returns>
        public static object DefaultForType(Type toDefault)
        {
            if (!toDefault.IsValueType
                || Nullable.GetUnderlyingType(toDefault) != null)
            {
                return null;// nullable types
            }
            return Activator.CreateInstance(toDefault);//non-nullable type
        }

        /// <summary>
        /// Creates a default instance of a provided type for use with populating out parameters when exceptions are thrown
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <returns>Default value of provided type</returns>
        public static T DefaultForType<T>()
        {
            return (T)DefaultForType(typeof(T));
        }

        /// <summary>
        /// MethodInfo for the generic-typed Helpers.DefaultForType(of T); this can be used for compiling dynamic expressions
        /// </summary>
        public static readonly MethodInfo DefaultForTypeInfo = typeof(Helpers)
            .GetMethod("DefaultForType",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(Type) },
                null);

        /// <summary>
        /// Generic-typed method to run Convert.ChangeType on an object to convert it to the generic type; will throw exceptions on conversion failure or trying to convert null to a non-nullable type
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="toConvert">Object to convert</param>
        /// <returns>Returns converted object</returns>
        public static T ConvertTo<T>(object toConvert)
        {
            return (T)ConvertTo(toConvert, typeof(T));
        }

        /// <summary>
        /// Runs Convert.ChangeType on an object to convert it to the specified type; will throw exceptions on conversion failure; will return a null for null input even if the specified type is non-nullable
        /// </summary>
        /// <typeparam name="T">Type to convert to</typeparam>
        /// <param name="toConvert">Object to convert</param>
        /// <returns>Returns converted object</returns>
        public static object ConvertTo(object toConvert, Type newType)
        {
            if (toConvert == null)
            {
                return null;
            }

            return Convert.ChangeType(toConvert, Nullable.GetUnderlyingType(newType) ?? newType);
        }

        /// <summary>
        /// MethodInfo for the generic-typed Helpers.ConvertTo(of T); this can be used for compiling dynamic expressions
        /// </summary>
        public static readonly MethodInfo ConvertToInfo = typeof(Helpers)
            .GetMethod("ConvertTo",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(object) },
                null);
    }
}