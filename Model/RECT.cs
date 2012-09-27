//
// RECT.cs
// Cloud Windows
//
// Created By BobS.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace win_client.Model
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        public override string ToString()
        {
            return ("Left :" + left.ToString() + "," + "Top :" + top.ToString() + "," + "Right :" + right.ToString() + "," + "Bottom :" + bottom.ToString());
        }
    }
}
