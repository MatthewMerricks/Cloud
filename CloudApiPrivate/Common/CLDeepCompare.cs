//
//  CLDeepCompare.cs
//  Cloud Windows
//
// From: http://mohammad-rahman.blogspot.com/2011/04/deep-objects-comparison.html
//  Created by BobS
//  Changes Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CloudApiPrivate.Common
{
    public static class CLDeepCompare
    {
        public static bool IsEqual(object firstObject, object secondObject)
        {
            if (firstObject == null && secondObject == null)
            {
                return true;
            }
            if (firstObject == null || secondObject == null)
            {
                return false;
            }

            bool result = default(bool);
            foreach (PropertyInfo firstObjectPropertyInfo in firstObject.GetType().GetProperties())
            {
                foreach (PropertyInfo secondObjectPropertyInfo in secondObject.GetType().GetProperties())
                {
                    if (firstObjectPropertyInfo.Name == secondObjectPropertyInfo.Name)
                    {
                        object firstObjectValue = firstObjectPropertyInfo.GetValue(firstObject, null);
                        object secondObjectValue = secondObjectPropertyInfo.GetValue(secondObject, null);

                        if (firstObject == null && secondObject == null)
                        {
                            result = true;
                        }
                        else if (firstObject == null || secondObject == null)
                        {
                            result = false;
                        }
                        else
                        {
                            result = firstObjectPropertyInfo.GetValue(firstObject, null).ToString() == secondObjectPropertyInfo.GetValue(secondObject, null).ToString();
                        }
                        
                        if (!result) break;
                    }
                }
                if (!result) break;
            }
            return result;
        }
    }
}
