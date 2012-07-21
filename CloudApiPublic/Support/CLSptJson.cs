//
//  CLSptJson.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Resources;
using System.Reflection;
using Newtonsoft.Json;

namespace CloudApiPublic.Support
{
    public static class CLSptJson
    {
        /// <summary>
        /// Deserialize JSON to a hierarchical Dictionary<string, object>.
        /// </summary>
        static public Dictionary<string, object> CLSptJsonDeserializeToDictionary(string jo) 
        { 
            Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(jo); 
            Dictionary<string, object> values2 = new Dictionary<string, object>(); 
            foreach (KeyValuePair<string, object> d in values) 
            { 
                if (d.Value != null && d.Value.GetType().FullName.Contains("Newtonsoft.Json.Linq.JObject")) 
                { 
                    values2.Add(d.Key, CLSptJsonDeserializeToDictionary(d.Value.ToString())); 
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
