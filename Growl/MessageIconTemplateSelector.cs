//
//  MessageIconTemplateSelector.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using CloudApiPublic.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace win_client.Growl
{
    public class MessageIconTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Nullable<EventMessageImage> castItem = item as Nullable<EventMessageImage>;

            if (castItem == null)
            {
                return null;
            }

            switch ((EventMessageImage)castItem)
            {
                case EventMessageImage.Busy:
                    return SyncingImage;
                case EventMessageImage.Completion:
                    return SyncedImage;
                case EventMessageImage.Error:
                    return FailedImage;
                case EventMessageImage.Inaction:
                    return SelectiveImage;
                case EventMessageImage.Informational:
                    return InformationalImage;
                default:
                //case EventMessageImage.NoImage:
                    return null;
            }
        }

        private static DataTemplate SyncingImage
        {
            get
            {
                return (_syncingImage = _syncingImage
                    ?? (DataTemplate)Application.Current.Resources["SyncingImage"]);
            }
        }
        private static DataTemplate _syncingImage = null;

        private static DataTemplate SyncedImage
        {
            get
            {
                return (_syncedImage = _syncedImage
                    ?? (DataTemplate)Application.Current.Resources["SyncedImage"]);
            }
        }
        private static DataTemplate _syncedImage = null;

        private static DataTemplate SelectiveImage
        {
            get
            {
                return (_selectiveImage = _selectiveImage
                    ?? (DataTemplate)Application.Current.Resources["SelectiveImage"]);
            }
        }
        private static DataTemplate _selectiveImage = null;

        private static DataTemplate FailedImage
        {
            get
            {
                return (_failedImage = _failedImage
                    ?? (DataTemplate)Application.Current.Resources["FailedImage"]);
            }
        }
        private static DataTemplate _failedImage = null;

        private static DataTemplate InformationalImage
        {
            get
            {
                return (_informationalImage = _informationalImage
                    ?? (DataTemplate)Application.Current.Resources["InformationalImage"]);
            }
        }
        private static DataTemplate _informationalImage = null;
    }
}