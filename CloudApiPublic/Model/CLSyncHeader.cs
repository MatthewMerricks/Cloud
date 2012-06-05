﻿//
//  CLSyncHeader.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.Model
{
    public class CLSyncHeader
    {
        public string Action { get; set; }
        public string EventID { get; set; }
        public string Sid { get; set; }
        public string Status { get; set; }
    }
}
