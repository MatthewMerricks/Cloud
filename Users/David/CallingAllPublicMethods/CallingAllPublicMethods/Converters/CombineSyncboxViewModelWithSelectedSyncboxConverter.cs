using CallingAllPublicMethods.Models;
using CallingAllPublicMethods.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CallingAllPublicMethods.Converters
{
    public sealed class CombineSyncboxViewModelWithSelectedSyncboxConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return new KeyValuePair<SyncboxViewModel, CLSyncboxProxy>((SyncboxViewModel)values[0], (CLSyncboxProxy)values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            KeyValuePair<SyncboxViewModel, CLSyncboxProxy> castValue = (KeyValuePair<SyncboxViewModel, CLSyncboxProxy>)value;
            return new object[] { castValue.Key, castValue.Value };
        }
    }
}