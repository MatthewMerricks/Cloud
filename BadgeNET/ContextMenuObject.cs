//
//  CLPreferences.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BadgeNET
{
    [Serializable]
    public class RECT
    {
        public Int32 left;
        public Int32 top;
        public Int32 right;
        public Int32 bottom;
    }

    [Serializable]
    public class ContextMenuObject
    {
        public RECT rectExplorerWindowCoordinates { get; set; }
        public List<string> asSelectedPaths { get; set; }
    }
}
