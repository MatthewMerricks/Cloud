using CallingAllPublicMethods.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CallingAllPublicMethods.Converters
{
    public sealed class CombineStoragePlanWithFriendlyNameConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return new KeyValuePair<CLStoragePlanProxy, string>((CLStoragePlanProxy)values[0], (string)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            KeyValuePair<CLStoragePlanProxy, string> castValue = (KeyValuePair<CLStoragePlanProxy, string>)value;
            return new object[] { castValue.Key, castValue.Value };
        }
    }
}