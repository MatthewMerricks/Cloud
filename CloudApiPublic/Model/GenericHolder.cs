//
//  GenericHolder.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.using System;
//
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudApiPublic.Model
{
    public sealed class GenericHolder<T>
    {
        public T Value { get; set; }

        /// <summary>
        /// Attempts to cast the first parameter as the current generic typed GenericHolder and sets Value to the second parameter
        /// </summary>
        /// <param name="toSet">Generic-typed GenericHolder for setting Value</param>
        /// <param name="value">Generic-type instance to put in Value</param>
        public static void GenericSet(object toSet, T value)
        {
            GenericSet(toSet as GenericHolder<T>, value);
        }

        /// <summary>
        /// Sets Value of the first parameter to the second parameter
        /// </summary>
        /// <param name="toSet">Holder of Value</param>
        /// <param name="value">Instance to put in Value</param>
        public static void GenericSet(GenericHolder<T> toSet, T value)
        {
            if (toSet != null)
            {
                toSet.Value = value;
            }
        }

        public GenericHolder() { }
        public GenericHolder(T defaultValue)
        {
            this.Value = defaultValue;
        }
    }
}
