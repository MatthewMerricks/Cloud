//
//  CLSptNSOperation.cs
//  Cloud SDK Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudApiPublic.Support
{
    public abstract class CLSptNSOperation
    {
        private bool _executing = false;
        public bool Executing
        {
            get { return _executing; }
            set { _executing = value; }
        }

        public CLSptNSOperation()
        {
        }

        abstract public void Cancel();

        abstract public void Main();
    }
}
