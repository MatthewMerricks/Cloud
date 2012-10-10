//
//  IconPathToImageSourceConverter.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using win_client.Model;

namespace win_client.Converters
{
    [ValueConversion(typeof(IconPath), typeof(ImageSource))]
    public class IconPathToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            string castValue;
            Nullable<int> overrideIndex;
            if (value is string)
            {
                castValue = (string)value;
                overrideIndex = null;
            }
            else if (value is IconPath)
            {
                castValue = ((IconPath)value).Path;
                overrideIndex = ((IconPath)value).FrameIndex;
            }
            else
            {
                castValue = value.ToString();
                overrideIndex = null;
            }
            
            Uri iconUri = new Uri(castValue, UriKind.Absolute);
            try
            {
                using (System.IO.Stream iconStream = Application.GetResourceStream(iconUri).Stream)
                {
                    IconBitmapDecoder iconDecoder = new IconBitmapDecoder(iconStream,
                        BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);

                    if (overrideIndex == null)
                    {
                        //prioritize bitdepth over pixel height
                        int bestIndex = -1;
                        int bitDepthOfBest = -1;
                        int heightOfBest = -1;

                        for (int iconFrameIndex = 0; iconFrameIndex < iconDecoder.Frames.Count; iconFrameIndex++)
                        {
                            BitmapFrame toCompare = iconDecoder.Frames[iconFrameIndex];
                            if (toCompare.Thumbnail.Format.BitsPerPixel > bitDepthOfBest)
                            {
                                bestIndex = iconFrameIndex;
                                bitDepthOfBest = toCompare.Thumbnail.Format.BitsPerPixel;
                                heightOfBest = toCompare.PixelHeight;
                            }
                            else if (toCompare.Thumbnail.Format.BitsPerPixel == bitDepthOfBest
                                && toCompare.PixelHeight > heightOfBest)
                            {
                                bestIndex = iconFrameIndex;
                                heightOfBest = toCompare.PixelHeight;
                            }
                        }

                        if (bestIndex == -1)
                        {
                            MessageBox.Show("Icon at Uri does not contain any valid Frames: " + castValue);
                            return null;
                        }

                        overrideIndex = bestIndex;
                    }

                    return iconDecoder.Frames[(int)overrideIndex];
                }
            }
            catch (IOException)
            {
                MessageBox.Show("Icon at Uri not found: " + castValue);
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unknown error loading icon at Uri: " + castValue + Environment.NewLine + ex.Message);
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}