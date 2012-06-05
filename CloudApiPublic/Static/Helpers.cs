//
// Helpers.cs
// Cloud SDK Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Static
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
            Type findStruct = toDefault;
            while (true)
            {
                if (findStruct == typeof(ValueType))
                {
                    return Activator.CreateInstance(toDefault);
                }
                else if (findStruct == typeof(object))
                {
                    return null;
                }
                findStruct = findStruct.BaseType;
            }
        }
    }
}