//
//  DoubleSecondsToDurationConverter.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace win_client.Converters
{
    [ValueConversion(typeof(double), typeof(Duration))]
    public class DoubleSecondsToDurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Duration.Forever;
            }

            double castValue;
            if (value is double)
            {
                castValue = (double)value;
            }
            else
            {
                string valueString;
                if (value is string)
                {
                    valueString = (string)value;
                }
                else
                {
                    valueString = value.ToString();
                }

                if (!double.TryParse(valueString, out castValue))
                {
                    return Duration.Forever;
                }
            }

            if (double.IsNaN(castValue)
                || double.IsInfinity(castValue)
                || castValue < 0)
            {
                return Duration.Forever;
            }

            return new Duration(TimeSpan.FromSeconds(castValue));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Nullable<Duration> castValue = value as Nullable<Duration>;

            if (castValue == null)
            {
                return double.NaN;
            }

            return ((Duration)castValue).TimeSpan.TotalSeconds;
        }
    }
}