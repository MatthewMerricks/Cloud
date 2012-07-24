// <copyright file="SecurityAlgorithmConverter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.Configuration
{
    using System;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Globalization;
    using System.ServiceModel.Security;
    using Microsoft.ServiceModel.Interop.Properties;

    /// <summary>
    /// Type converter implementation for security algorithms
    /// </summary>
    internal class SecurityAlgorithmSuiteConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the
        /// given source type to a Security algorithm suite using the specified context.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="sourceType">A System.Type that represents the type you wish to convert from.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (typeof(string) == sourceType) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the
        /// given destination type using the context.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="destinationType">A System.Type that represents the type you wish to convert to.</param>
        /// <returns>true if this converter can perform the conversion; otherwise, false.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return (typeof(InstanceDescriptor) == destinationType) || base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to a Security Algorithm Suite.
        /// </summary>
        /// <param name="context">An System.ComponentModel.ITypeDescriptorContext that provides a format context.</param>
        /// <param name="culture">An optional System.Globalization.CultureInfo. If not supplied, the current
        /// culture is assumed.</param>
        /// <param name="value">The System.Object to convert.</param>
        /// <returns>An System.Object that represents the converted value.</returns>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str == null)
            {
                return base.ConvertFrom(context, culture, value);
            }

            switch (str)
            {
                case "Default":
                    return SecurityAlgorithmSuite.Default;

                case "Basic256":
                    return SecurityAlgorithmSuite.Basic256;

                case "Basic192":
                    return SecurityAlgorithmSuite.Basic192;

                case "Basic128":
                    return SecurityAlgorithmSuite.Basic128;

                case "TripleDes":
                    return SecurityAlgorithmSuite.TripleDes;

                case "Basic256Rsa15":
                    return SecurityAlgorithmSuite.Basic256Rsa15;

                case "Basic192Rsa15":
                    return SecurityAlgorithmSuite.Basic192Rsa15;

                case "Basic128Rsa15":
                    return SecurityAlgorithmSuite.Basic128Rsa15;

                case "TripleDesRsa15":
                    return SecurityAlgorithmSuite.TripleDesRsa15;

                case "Basic256Sha256":
                    return SecurityAlgorithmSuite.Basic256Sha256;

                case "Basic192Sha256":
                    return SecurityAlgorithmSuite.Basic192Sha256;

                case "Basic128Sha256":
                    return SecurityAlgorithmSuite.Basic128Sha256;

                case "TripleDesSha256":
                    return SecurityAlgorithmSuite.TripleDesSha256;

                case "Basic256Sha256Rsa15":
                    return SecurityAlgorithmSuite.Basic256Sha256Rsa15;

                case "Basic192Sha256Rsa15":
                    return SecurityAlgorithmSuite.Basic192Sha256Rsa15;

                case "Basic128Sha256Rsa15":
                    return SecurityAlgorithmSuite.Basic128Sha256Rsa15;

                case "TripleDesSha256Rsa15":
                    return SecurityAlgorithmSuite.TripleDesSha256Rsa15;
            }

            throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.InvariantCulture, Strings.Not_Supported_Algorithm, typeof(SecurityAlgorithmSuite).FullName));
        }

        /// <summary>
        /// Converts the given object to another type.
        /// </summary>
        /// <param name="context">A formatter context.</param>
        /// <param name="culture">The culture into which value will be converted.</param>
        /// <param name="value">The object to convert.</param>
        /// <param name="destinationType">The type to convert the object to.</param>
        /// <returns>The converted object.</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            SecurityAlgorithmSuite suite = value as SecurityAlgorithmSuite;

            if (!(typeof(string) == destinationType) || suite == null)
            {
                return base.ConvertTo(context, culture, value, destinationType);
            }

            if (suite == SecurityAlgorithmSuite.Default)
            {
                return "Default";
            }

            if (suite == SecurityAlgorithmSuite.Basic256)
            {
                return "Basic256";
            }

            if (suite == SecurityAlgorithmSuite.Basic192)
            {
                return "Basic192";
            }

            if (suite == SecurityAlgorithmSuite.Basic128)
            {
                return "Basic128";
            }

            if (suite == SecurityAlgorithmSuite.TripleDes)
            {
                return "TripleDes";
            }

            if (suite == SecurityAlgorithmSuite.Basic256Rsa15)
            {
                return "Basic256Rsa15";
            }

            if (suite == SecurityAlgorithmSuite.Basic192Rsa15)
            {
                return "Basic192Rsa15";
            }

            if (suite == SecurityAlgorithmSuite.Basic128Rsa15)
            {
                return "Basic128Rsa15";
            }

            if (suite == SecurityAlgorithmSuite.TripleDesRsa15)
            {
                return "TripleDesRsa15";
            }

            if (suite == SecurityAlgorithmSuite.Basic256Sha256)
            {
                return "Basic256Sha256";
            }

            if (suite == SecurityAlgorithmSuite.Basic192Sha256)
            {
                return "Basic192Sha256";
            }

            if (suite == SecurityAlgorithmSuite.Basic128Sha256)
            {
                return "Basic128Sha256";
            }

            if (suite == SecurityAlgorithmSuite.TripleDesSha256)
            {
                return "TripleDesSha256";
            }

            if (suite == SecurityAlgorithmSuite.Basic256Sha256Rsa15)
            {
                return "Basic256Sha256Rsa15";
            }

            if (suite == SecurityAlgorithmSuite.Basic192Sha256Rsa15)
            {
                return "Basic192Sha256Rsa15";
            }

            if (suite == SecurityAlgorithmSuite.Basic128Sha256Rsa15)
            {
                return "Basic128Sha256Rsa15";
            }

            if (suite != SecurityAlgorithmSuite.TripleDesSha256Rsa15)
            {
                throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.InvariantCulture, Strings.Not_Supported_Algorithm, typeof(SecurityAlgorithmSuite).FullName));
            }

            return "TripleDesSha256Rsa15";
        }
    }
}