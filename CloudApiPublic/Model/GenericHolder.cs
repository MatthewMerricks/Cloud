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

        public static void GenericSet(object toSet, T value)
        {
            GenericHolder<T> castSet = toSet as GenericHolder<T>;
            if (castSet != null)
            {
                castSet.Value = value;
            }
        }
    }
}
