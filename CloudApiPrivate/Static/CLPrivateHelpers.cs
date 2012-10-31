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
using CloudApiPublic.Model;
using System.Reflection;
using Newtonsoft.Json;

namespace CloudApiPrivate.Static
{
    public class CLPrivateHelpers
    {
        /// <summary>
        /// Get the friendly name of this computer.
        /// </summary>
        /// <returns></returns>
        public static string GetComputerFriendlyName()
        {
            // Todo: should find an algorithm to generate a unique identifier for this device name
            return Environment.MachineName;
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes == 1)
            {
                return "1 Byte"; // special case to remove the plural
            }

            const int scale = 1024;
            long max = (long)Math.Pow(scale, FormatBytesOrders.Length - 1);

            foreach (string order in FormatBytesOrders)
            {
                if (bytes > max)
                {
                    return string.Format("{0:##.##} {1}", decimal.Divide(bytes, max), order);
                }
                else if (bytes == max)
                {
                    return string.Format("1 {0}", order);
                }

                max /= scale;
            }
            return "0 Bytes"; // default for bytes that are less than or equal to zero
        }
        private static readonly string[] FormatBytesOrders = new string[] { "GB", "MB", "KB", "Bytes" };

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
