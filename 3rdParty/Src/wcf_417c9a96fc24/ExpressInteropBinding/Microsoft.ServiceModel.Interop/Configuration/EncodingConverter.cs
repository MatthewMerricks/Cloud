// <copyright file="EncodingConverter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Configuration
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Globalization;
    using System.Text;
    using Microsoft.ServiceModel.Interop.Properties;

    /// <summary>
    /// Type converter implementation for text encodings
    /// </summary>
    internal class EncodingConverter : TypeConverter
    {
        /// <summary>
        /// Returns whether this converter can convert an object of the given type to
        //  the type of this converter.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="sourceType">A System.Type that represents the type you want to convert from.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (typeof(string) == sourceType) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///  Returns whether this converter can convert the object to the specified type,
        //   using the specified context.
        /// </summary>
        /// <param name="context"> An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="destinationType">A System.Type that represents the type you want to convert to.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (typeof(InstanceDescriptor) == destinationType) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to the type of this converter, using the specified
        //  context and culture information.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">The System.Globalization.CultureInfo to use as the current culture.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string stringValue = value as string;

            Encoding encoding;
            if (stringValue == null)
            {
                return base.ConvertFrom(context, culture, value);
            }

            if (string.Compare(stringValue, "utf-8", StringComparison.OrdinalIgnoreCase) == 0)
            {
                encoding = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            }
            else
            {
                encoding = Encoding.GetEncoding(stringValue);
            }

            if (encoding == null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, Strings.Invalid_Encoding, stringValue));
            }

            return encoding;
        }

        /// <summary>
        /// Converts the given value object to the specified type, using the specified
        //  context and culture information.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">A System.Globalization.CultureInfo. If null is passed, the current culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <param name="destinationType">The System.Type to convert the value parameter to.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Encoding encodingValue = value as Encoding;

            if ((typeof(string) == destinationType) && (encodingValue != null))
            {
                return encodingValue.HeaderName;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

