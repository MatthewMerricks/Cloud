//
//  CLJsonResultWithError.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloud.Model
{
    internal class CLJsonResultWithError
    {
        public Dictionary<string, object> JsonResult;
        public CLError Error;
    }
}