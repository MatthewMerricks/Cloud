﻿using CloudApiPublic;
using CloudApiPublic.Model;
using CloudSDK_SmokeTest.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudSDK_SmokeTest.Events.CLEventArgs
{
    public sealed class ItemListHelperEventArgs : CLEventArgs
    {
        public InputParams ParamSet { get; set; }
        public ListItems ListItemsTask { get; set; }
        public GenericHolder<CLError> ProcessingErrorHolder { get; set; }
    }
}