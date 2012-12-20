//
//  BindingAndTriggerValue.cs
//  Cloud Windows
//
//  Created by DavidBruck
//  Copyright (c) Cloud.com. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace CloudApiPublic.EventMessageReceiver
{
    public class BindingAndTriggerValue
    {
        public Binding Binding { get; set; }
        public object Value { get; set; }
    }
}