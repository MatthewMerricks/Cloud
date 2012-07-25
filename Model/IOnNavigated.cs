//
//  IOnNavigated.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using CloudApiPublic.Model;

namespace win_client.Model
{
    public interface IOnNavigated
    {   
        CLError HandleNavigated(object sender, NavigationEventArgs e);
    }
}
