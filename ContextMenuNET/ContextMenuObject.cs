//
//  ContextMenuObject.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ContextMenuNET
{
    [Serializable]
    public class RECT
    {
        public Int32 left;
        public Int32 top;
        public Int32 right;
        public Int32 bottom;

        public RECT()
        {
            left = 0;
            top = 0;
            right = 0;
            bottom = 0;
        }
    }

    [Serializable]
    public class ContextMenuObject
    {
        public RECT rectExplorerWindowCoordinates { get; set; }
        public List<string> asSelectedPaths { get; set; }

        public ContextMenuObject()
        {
            rectExplorerWindowCoordinates = new RECT();
            asSelectedPaths = new List<string>();
        }
    }
}
