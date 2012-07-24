// <copyright file="FormUrlEncodedExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Web
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Json;
    using System.Linq;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Web;

    /// <summary>
    /// This class provides helper methods for decoding form url-encoded strings.
    /// </summary>
    public static class FormUrlEncodedExtensions
    {
        /// <summary>
        /// Returns the query string from the incoming web context as a <see cref="System.Json.JsonObject"/> instance.
        /// </summary>
        /// <param name="context">The <see cref="System.ServiceModel.Web.IncomingWebRequestContext"/> instance
        /// where the query string can be retrieved.</param>
        /// <returns>The query string parsed as a <see cref="System.Json.JsonObject"/> instance.</returns>
        /// <remarks>The main usage of this extension method is to retrieve the query string within
        /// an operation using the System.ServiceModel.Web.WebOperationContext.Current.IncomingContext object.
        /// The query string is parsed as x-www-form-urlencoded data.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "0",
            Justification = "Call to DiagnosticUtility validates the parameter.")]
        public static JsonObject GetQueryStringAsJsonObject(this IncomingWebRequestContext context)
        {
            DiagnosticUtility.ExceptionUtility.ThrowOnNull(context, "context");

            string query = context.UriTemplateMatch.RequestUri.Query;
            return ParseFormUrlEncoded(query);
        }

        /// <summary>
        /// Parses a query string (x-www-form-urlencoded) as a <see cref="System.Json.JsonObject"/>.
        /// </summary>
        /// <param name="queryString">The query string to be parsed.</param>
        /// <returns>The <see cref="System.Json.JsonObject"/> corresponding to the given query string.</returns>
        public static JsonObject ParseFormUrlEncoded(string queryString)
        {
            return ParseFormUrlEncoded(queryString, int.MaxValue);
        }

        /// <summary>
        /// Parses a query string (x-www-form-urlencoded) as a <see cref="System.Json.JsonObject"/>.
        /// </summary>
        /// <param name="queryString">The query string to be parsed.</param>
        /// <param name="maxDepth">The maximum depth of object graph encoded as x-www-form-urlencoded.</param>
        /// <returns>The <see cref="System.Json.JsonObject"/> corresponding to the given query string.</returns>
        public static JsonObject ParseFormUrlEncoded(string queryString, int maxDepth)
        {
            DiagnosticUtility.ExceptionUtility.ThrowOnNull(queryString, "queryString");
            return ParseFormUrlEncoded(ParseQueryString(queryString), maxDepth);
        }

        /// <summary>
        /// Parses a collection of query string values as a <see cref="System.Json.JsonObject"/>.
        /// </summary>
        /// <param name="queryStringValues">The collection of query string values.</param>
        /// <param name="maxDepth">The maximum depth of object graph encoded as x-www-form-urlencoded.</param>
        /// <returns>The <see cref="System.Json.JsonObject"/> corresponding to the given query string values.</returns>
        internal static JsonObject ParseFormUrlEncoded(IEnumerable<Tuple<string, string>> queryStringValues, int maxDepth)
        {
            DiagnosticUtility.ExceptionUtility.ThrowOnNull(queryStringValues, "queryString");
            return FormUrlEncodedExtensions.Parse(queryStringValues, maxDepth);
        }

        internal static JsonObject Parse(IEnumerable<Tuple<string, string>> nameValuePairs, int maxDepth)
        {
            JsonObject result = new JsonObject();
            foreach (var nameValuePair in nameValuePairs)
            {
                if (nameValuePair.Item1 == null)
                {
                    if (string.IsNullOrEmpty(nameValuePair.Item2))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new ArgumentException(DiagnosticUtility.GetString(SR.QueryStringNameShouldNotNull), "nameValuePairs"));
                    }

                    string[] path = new string[] { nameValuePair.Item2 };
                    Insert(result, path, null);
                }
                else
                {
                    string[] path = GetPath(nameValuePair.Item1, maxDepth);
                    Insert(result, path, nameValuePair.Item2);
                }
            }

            FixContiguousArrays(result);
            return result;
        }

        private static string[] GetPath(string key, int maxDepth)
        {
            Debug.Assert(key != null, "Key cannot be null (this function is only called by Parse if key != null)");

            if (string.IsNullOrEmpty(key))
            {
                return new string[] { string.Empty };
            }

            ValidateQueryString(key);
            string[] path = key.Split('[');
            for (int i = 0; i < path.Length; i++)
            {
                if (path[i].EndsWith("]", StringComparison.Ordinal))
                {
                    path[i] = path[i].Substring(0, path[i].Length - 1);
                }
            }

            // For consistency with JSON, the depth of a[b]=1 is 3 (which is the depth of {a:{b:1}}, given
            // that in the JSON-XML mapping there's a <root> element wrapping the JSON object:
            // <root><a><b>1</b></a></root>. So if the length of the path is greater than *or equal* to
            // maxDepth, then we throw.
            if (path.Length >= maxDepth)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(DiagnosticUtility.GetString(SR.MaxDepthExceeded, maxDepth)));
            }

            return path;
        }

        private static void ValidateQueryString(string key)
        {
            bool hasUnMatchedLeftBraket = false;
            for (int i = 0; i < key.Length; i++)
            {
                switch (key[i])
                {
                    case '[':
                        if (!hasUnMatchedLeftBraket)
                        {
                            hasUnMatchedLeftBraket = true;
                        }
                        else
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(DiagnosticUtility.GetString(SR.NestedBracketNotValid, i)));
                        }

                        break;
                    case ']':
                        if (hasUnMatchedLeftBraket)
                        {
                            hasUnMatchedLeftBraket = false;
                        }
                        else
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(DiagnosticUtility.GetString(SR.UnMatchedBracketNotValid, i)));
                        }

                        break;
                }
            }

            if (hasUnMatchedLeftBraket)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(DiagnosticUtility.GetString(SR.NestedBracketNotValid, key.LastIndexOf('['))));
            }
        }

        private static void Insert(JsonObject root, string[] path, string value)
        {
            // to-do: verify consistent with new parsing, whether single value is in path or value
            Debug.Assert(root != null, "Root object can't be null");
            if (value == null)
            {
                Debug.Assert(path.Length == 1, "This should only be hit in the case of only a value-only query part");
                if (root.ContainsKey(path[0]) && root[path[0]] != null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ArgumentException(DiagnosticUtility.GetString(SR.FormUrlEncodedMismatchingTypes, BuildPathString(path, 0))));
                }

                root[path[0]] = null;
            }
            else
            {
                JsonObject current = root;
                JsonObject parent = null;

                for (int i = 0; i < path.Length - 1; i++)
                {
                    if (String.IsNullOrEmpty(path[i]))
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new ArgumentException(DiagnosticUtility.GetString(
                                SR.InvalidArrayInsert,
                                BuildPathString(path, i))));
                    }

                    if (!current.ContainsKey(path[i]))
                    {
                        current[path[i]] = new JsonObject();
                    }
                    else
                    {
                        // Since the loop goes up to the next-to-last item in the path, if we hit a null
                        // or a primitive, then we have a mismatching node.
                        if (current[path[i]] == null || current[path[i]] is JsonPrimitive)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                                new ArgumentException(
                                    DiagnosticUtility.GetString(
                                        SR.FormUrlEncodedMismatchingTypes,
                                        BuildPathString(path, i))));
                        }
                    }

                    parent = current;
                    current = current[path[i]] as JsonObject;
                }

                string lastKey = path[path.Length - 1];
                if (string.IsNullOrEmpty(lastKey) && path.Length > 1)
                {
                    AddToArray(parent, path, value);
                }
                else
                {
                    if (current == null)
                    {
                        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                            new ArgumentException(
                                DiagnosticUtility.GetString(
                                    SR.FormUrlEncodedMismatchingTypes,
                                    BuildPathString(path, path.Length - 1))));
                    }

                    AddToObject(current, path, value);
                }
            }
        }

        private static void AddToObject(JsonObject obj, string[] path, string value)
        {
            int pathIndex = path.Length - 1;
            string key = path[pathIndex];

            if (obj.ContainsKey(key))
            {
                if (obj[key] == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(DiagnosticUtility.GetString(SR.FormUrlEncodedMismatchingTypes, BuildPathString(path, pathIndex))));
                }

                bool isRoot = path.Length == 1;
                if (isRoot)
                {
                    // jQuery 1.3 behavior, make it into an array(object) if primitive
                    if (obj[key].JsonType == JsonType.String)
                    {
                        string oldValue = obj[key].ReadAs<string>();
                        JsonObject jo = new JsonObject();
                        jo.Add("0", oldValue);
                        jo.Add("1", value);
                        obj[key] = jo;
                    }
                    else if (obj[key] is JsonObject)
                    {
                        // if it was already an object, simply add the value
                        JsonObject jo = obj[key] as JsonObject;
                        jo.Add(GetIndex(jo.Keys), value);
                    }
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(DiagnosticUtility.GetString(SR.JQuery13CompatModeNotSupportNestedJson, BuildPathString(path, pathIndex))));
                }
            }
            else
            {
                // if the object didn't contain the key, simply add it now
                obj[key] = value;
            }
        }

        // JsonObject passed in is semantically an array
        private static void AddToArray(JsonObject parent, string[] path, string value)
        {
            Debug.Assert(path.Length >= 2, "The path must be at least 2, one for the ending [], and one for before the '[' (which can be empty)");

            string parentPath = path[path.Length - 2];

            Debug.Assert(parent.ContainsKey(parentPath), "It was added on insert to get to this point");
            JsonObject jo = parent[parentPath] as JsonObject;

            if (jo == null)
            {
                // a[b][c]=1&a[b][]=2 => invalid
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new ArgumentException(DiagnosticUtility.GetString(
                        SR.FormUrlEncodedMismatchingTypes,
                        BuildPathString(path, path.Length - 1))));
            }
            else
            {
                jo.Add(GetIndex(jo.Keys), value);
            }
        }

        // to-do: consider optimize it by only look at the last one
        private static string GetIndex(IEnumerable<string> keys)
        {
            int max = -1;
            foreach (var key in keys)
            {
                int tempInt;
                if (int.TryParse(key, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out tempInt) && tempInt > max)
                {
                    max = tempInt;
                }
                else
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ArgumentException(DiagnosticUtility.GetString(
                            SR.FormUrlEncodedMismatchingTypes,
                            key)));
                }
            }

            max++;
            return max.ToString(CultureInfo.InvariantCulture);
        }

        private static void FixContiguousArrays(JsonValue jv)
        {
            JsonArray ja = jv as JsonArray;

            if (ja != null)
            {
                for (int i = 0; i < ja.Count; i++)
                {
                    if (ja[i] != null)
                    {
                        ja[i] = FixSingleContiguousArray(ja[i]);
                        FixContiguousArrays(ja[i]);
                    }
                }
            }
            else
            {
                JsonObject jo = jv as JsonObject;

                if (jo != null)
                {
                    List<string> keys = new List<string>(jo.Keys);
                    foreach (string key in keys)
                    {
                        if (jo[key] != null)
                        {
                            jo[key] = FixSingleContiguousArray(jo[key]);
                            FixContiguousArrays(jo[key]);
                        }
                    }
                }
            }

            //// do nothing for primitives
        }

        private static JsonValue FixSingleContiguousArray(JsonValue original)
        {
            JsonObject jo = original as JsonObject;
            if (jo != null && jo.Count > 0)
            {
                List<string> childKeys = new List<string>(jo.Keys);
                List<string> sortedKeys;
                if (CanBecomeArray(childKeys, out sortedKeys))
                {
                    JsonArray newResult = new JsonArray();
                    foreach (string sortedKey in sortedKeys)
                    {
                        newResult.Add(jo[sortedKey]);
                    }

                    return newResult;
                }
            }

            return original;
        }

        private static bool CanBecomeArray(List<string> keys, out List<string> sortedKeys)
        {
            List<KeyValuePair<int, string>> intKeys = new List<KeyValuePair<int, string>>();
            sortedKeys = null;
            bool areContiguousIndices = true;
            foreach (string key in keys)
            {
                int intKey;
                if (!int.TryParse(key, NumberStyles.None, CultureInfo.InvariantCulture, out intKey))
                {
                    // if not a non-negative number, it cannot become an array
                    areContiguousIndices = false;
                    break;
                }

                string strKey = intKey.ToString(CultureInfo.InvariantCulture);
                if (!strKey.Equals(key, StringComparison.Ordinal))
                {
                    // int.Parse returned true, but it's not really the same number.
                    // It's the case for strings such as "1\0".
                    areContiguousIndices = false;
                    break;
                }

                intKeys.Add(new KeyValuePair<int, string>(intKey, strKey));
            }

            if (areContiguousIndices)
            {
                intKeys.Sort((x, y) => x.Key - y.Key);

                for (int i = 0; i < intKeys.Count; i++)
                {
                    if (intKeys[i].Key != i)
                    {
                        areContiguousIndices = false;
                        break;
                    }
                }
            }

            if (areContiguousIndices)
            {
                sortedKeys = new List<string>(intKeys.Select(x => x.Value));
            }

            return areContiguousIndices;
        }

        private static string BuildPathString(string[] path, int i)
        {
            StringBuilder errorPath = new StringBuilder(path[0]);
            for (int p = 1; p <= i; p++)
            {
                errorPath.AppendFormat("[{0}]", path[p]);
            }

            return errorPath.ToString();
        }

        private static IEnumerable<Tuple<string, string>> ParseQueryString(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString))
            {
                if ((queryString.Length > 0) && (queryString[0] == '?'))
                {
                    queryString = queryString.Substring(1);
                }

                if (!string.IsNullOrEmpty(queryString))
                {
                    string[] pairs = queryString.Split('&');
                    foreach (string str in pairs)
                    {
                        string[] keyValue = str.Split('=');
                        if (keyValue.Length == 2)
                        {
                            yield return new Tuple<string, string>(HttpUtility.UrlDecode(keyValue[0]), HttpUtility.UrlDecode(keyValue[1]));
                        }
                        else if (keyValue.Length == 1)
                        {
                            yield return new Tuple<string, string>(null, HttpUtility.UrlDecode(keyValue[0]));
                        }
                    }
                }
            }
        }
    }
}