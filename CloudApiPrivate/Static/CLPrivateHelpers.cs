//
//  CLPrivateHelpers.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Cloud.Model;
using System.Reflection;
using Newtonsoft.Json;

namespace CloudApiPrivate.Static
{
    public class CLPrivateHelpers
    {
        /// <summary>
        /// Deserialize JSON to a hierarchical Dictionary<string, object>.
        /// </summary>
        static public Dictionary<string, object> JsonDeserializeToDictionary(string jo)
        {
            Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(jo);
            Dictionary<string, object> values2 = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> d in values)
            {
                if (d.Value != null && d.Value.GetType().FullName.Contains("Newtonsoft.Json.Linq.JObject"))
                {
                    values2.Add(d.Key, JsonDeserializeToDictionary(d.Value.ToString()));
                }
                else
                {
                    values2.Add(d.Key, d.Value);
                }
            }
            return values2;
        }
    }
}
