//
//  CLPublicExtensionMethods.cs
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
using System.Collections.Generic;


namespace CloudApiPublic.Static
{

    public static class CLPublicExtensionMethods
    {
        /// <summary> 
        /// Extend Dictionary to test for contained keys, and return default values if the key doesn't exist.
        /// </summary> 
        /// <param name="input">The dictionary to extend.</param> 
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">The default value to return if the key is not found.</param>
        /// <typeparam name="TKey">The type of the key.</typeparam> 
        /// <typeparam name="TValue">The type of the associated value.</typeparam> 
        /// <param name="childName">x:Name or Name of child. </param> 
        /// <returns>The value (if found), or the defaultValue.  
        /// 
        /// Call like this for a reference object:
        /// object result = myDictionary.GetValueOrDefault("key1") ?? myDefaultObject;
        ///     --OR--
        /// object result = myDictionary.GetValueOrDefault("key1", myDefaultObject);
        /// 
        /// Call like this for a value object:
        /// int i = anotherDictionary.GetValueOrDefault("key2", -1); 
        /// 

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> input, TKey key, TValue defaultValue = default(TValue))
        {
            TValue val;
            if (input.TryGetValue(key, out val))
            {
                return val;
            }

            return defaultValue;
        } 
    }
}
