// <copyright file="BooleanToVisibilityConverter.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ServiceModel.Interop.ConfigurationWizard.View
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Boolean to visibility type converter
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Initializes a new instance of the BooleanToVisibilityConverter class
        /// </summary>
        public BooleanToVisibilityConverter()
        {
            this.WhenTrue = Visibility.Visible;
            this.WhenFalse = Visibility.Collapsed;
        }

        /// <summary>
        /// Gets or sets the visibility settings when the converter takes a value equals to false
        /// </summary>
        public Visibility WhenFalse { get; set; }

        /// <summary>
        /// Gets or sets the visibility settings when the converter takes a value equals to true
        /// </summary>
        public Visibility WhenTrue { get; set; }

        /// <summary>
        /// Converts an object to a target type
        /// </summary>
        /// <param name="value">Object to convert</param>
        /// <param name="targetType">Target type</param>
        /// <param name="parameter">Custom parameter</param>
        /// <param name="culture">Culture info</param>
        /// <returns>A valid instance of the target type</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var nullable = value as bool?;
            if (!nullable.HasValue)
            {
                throw new ArgumentException("Value must be Boolean.", "value");
            }

            if (targetType != typeof(Visibility))
            {
                throw new ArgumentException("Must convert to Visibility.", "targetType");
            }

            return nullable.Value ? this.WhenTrue : this.WhenFalse;
        }

        /// <summary>
        /// Converts a value
        /// </summary>
        /// <param name="value">The value that is produced by the binding target</param>
        /// <param name="targetType">The type to convert to</param>
        /// <param name="parameter">The converter parameter to use</param>
        /// <param name="culture">The culture to use in the converter</param>
        /// <returns>A converted value. If the method returns null, the valid null value is used</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

