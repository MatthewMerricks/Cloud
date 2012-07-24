// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonValueEvents
{
    using System;
    using System.Globalization;
    using System.Json;
    using System.Windows.Data;

    public class JsonPrimitiveConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string parameterType = (string)parameter;
            JsonPrimitive jp = (JsonPrimitive)value;
            object result = null;

            switch (parameterType)
            {
                case "int":
                    result = jp.ReadAs<int>(0);
                    break;
                case "date":
                    result = jp.ReadAs<DateTime>(DateTime.Now);
                    break;
                default:
                    result = jp.ReadAs<string>();
                    break;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
