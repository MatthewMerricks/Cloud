// <copyright file="TimeSpanOrInfiniteConverter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Configuration
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.ServiceModel.Interop.Properties;

    /// <summary>
    /// Timespan type converter
    /// </summary>
    internal class TimeSpanOrInfiniteConverter : TimeSpanConverter
    {
        /// <summary>
        /// Converts the given object to a Timestamp
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="cultureInfo">An optional System.Globalization.CultureInfo. If not supplied, the current
        /// culture is assumed.</param>
        /// <param name="data">The System.Object to convert.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo cultureInfo, object data)
        {
            if (string.Equals((string)data, "infinite", StringComparison.OrdinalIgnoreCase))
            {
                return TimeSpan.MaxValue;
            }

            return base.ConvertFrom(context, cultureInfo, data);
        }

        /// <summary>
        /// Converts the given object to another type.
        /// </summary>
        /// <param name="context">A formatter context.</param>
        /// <param name="cultureInfo">The culture into which value will be converted.</param>
        /// <param name="value">The object to convert.</param>
        /// <param name="type">The type to convert the object to.</param>
        /// <returns>The converted object.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo cultureInfo, object value, Type type)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            
            if (!(value is TimeSpan))
            {
                throw new NotSupportedException(
                    string.Format(
                        CultureInfo.InvariantCulture, 
                        Strings.Conversion_Type_Not_Supported, 
                        value.GetType().FullName, 
                        typeof(TimeSpan).FullName));
            }

            if (((TimeSpan)value) == TimeSpan.MaxValue)
            {
                return "Infinite";
            }

            return base.ConvertTo(context, cultureInfo, value, type);
        }
    }
}