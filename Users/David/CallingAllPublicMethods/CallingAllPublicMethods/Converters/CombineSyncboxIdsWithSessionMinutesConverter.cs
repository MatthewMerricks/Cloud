using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CallingAllPublicMethods.Converters
{
    public sealed class CombineSyncboxIdsWithSessionMinutesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return new KeyValuePair<string, string>((string)values[0], (string)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            KeyValuePair<string, string> castValue = (KeyValuePair<string, string>)value;
            return new object[] { castValue.Key, castValue.Value };
        }
    }
}