using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CallingAllPublicMethods.Converters
{
    [ValueConversion(typeof(Nullable<long>), typeof(string))]
    public sealed class NullableLongToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value == null
                ? "{null}"
                : ((long)value).ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long parsedValue;
            if (value == null
                || !long.TryParse(value.ToString(), out parsedValue))
            {
                return (Nullable<long>)null;
            }
            return (Nullable<long>)parsedValue;
        }
    }
}