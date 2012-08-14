//
// CLConverters.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Globalization;
using win_client.ViewModels;
using CloudApiPrivate.Model.Settings;

namespace win_client.Common
{

    public class RadioButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value.ToString() == parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(StorageSizeSelections), parameter.ToString(), true) : null;
        }
    }

    public class RadioButtonUnselectedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value.ToString() != parameter.ToString())
            {
                return (object)Visibility.Visible;
            }
            else
            {
                return (object)Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Enum.Parse(typeof(StorageSizeSelections), parameter.ToString(), true) : null;
        }
    }

    public class BoolIsVisible2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return (object)Visibility.Visible;
            }
            else
            {
                return (object)Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((Visibility)value == Visibility.Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class QuotaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int? inputValue = value as int?;
            if (inputValue != null)
            {
                return (string)inputValue.ToString() + ".0GB Used";
            }
            else
            {
                return (string)String.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class DebugConverter : IValueConverter
    {
        public DebugConverter()
        {

        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    //public class EnumMatchToBooleanConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (targetType.IsAssignableFrom(typeof(Boolean)) && targetType.IsAssignableFrom(typeof(String)))
    //            throw new ArgumentException("EnumConverter can only convert to boolean or string.");
    //        if (targetType == typeof(String))
    //            return value.ToString();

    //        return String.Compare(value.ToString(), (String)parameter, StringComparison.InvariantCultureIgnoreCase) == 0;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        if (targetType.IsAssignableFrom(typeof(Boolean)) && targetType.IsAssignableFrom(typeof(String)))
    //            throw new ArgumentException("EnumConverter can only convert back value from a string or a boolean.");
    //        if (!targetType.IsEnum)
    //            throw new ArgumentException("EnumConverter can only convert value to an Enum Type.");

    //        if (value.GetType() == typeof(String))
    //        {
    //            return Enum.Parse(targetType, (String)value, true);
    //        }

    //        //We have a boolean, as for binding to a checkbox. we use parameter 
    //        if ((Boolean)value)
    //            return Enum.Parse(targetType, (String)parameter, true);

    //        return Binding.DoNothing;
    //    }
    //} 

    public class EnumMatchToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ParameterString = parameter as string;
            if (ParameterString == null)
            {
                return DependencyProperty.UnsetValue;
            }

            if (Enum.IsDefined(value.GetType(), value) == false)
            {
                return DependencyProperty.UnsetValue;
            }

            object paramvalue = Enum.Parse(value.GetType(), ParameterString);
            return paramvalue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var ParameterString = parameter as string;
            if (ParameterString == null)
            {
                return DependencyProperty.UnsetValue;
            }

            return Enum.Parse(targetType, ParameterString);
        }
    }
}
