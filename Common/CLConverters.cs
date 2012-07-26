﻿//
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
}
