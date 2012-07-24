﻿// <copyright file="JsonValueLinqExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.Json
{
    using System.Collections.Generic;

    /// <summary>
    /// This class extends the funcionality of the <see cref="JsonValue"/> type for better Linq support . 
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Linq is a technical name.")]
    public static class JsonValueLinqExtensions
    {
        /// <summary>
        /// Extension method for creating a <see cref="JsonValue"/> from an <see cref="IEnumerable{T}"/> collection of <see cref="JsonValue"/> types.
        /// </summary>
        /// <param name="items">The enumerable instance.</param>
        /// <returns>A <see cref="JsonArray"/> created from the specified items.</returns>
        public static JsonArray ToJsonArray(this IEnumerable<JsonValue> items)
        {
            return new JsonArray(items);
        }

        /// <summary>
        /// Extension method for creating a <see cref="JsonValue"/> from an <see cref="IEnumerable{T}"/> collection of <see cref="KeyValuePair{K,V}"/> of <see cref="String"/> and <see cref="JsonValue"/> types.
        /// </summary>
        /// <param name="items">The enumerable instance.</param>
        /// <returns>A <see cref="JsonValue"/> created from the specified items.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures", Justification = "JsonValue implements the nested type in param.")]
        public static JsonObject ToJsonObject(this IEnumerable<KeyValuePair<string, JsonValue>> items)
        {
            return new JsonObject(items);
        }
    }
}
