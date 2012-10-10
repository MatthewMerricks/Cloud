using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace win_client.Model
{
    public sealed class CLStatusFileTransferBlank : CLStatusFileTransferBase<CLStatusFileTransferBlank>
    {
        public override Visibility Visibility
        {
            get { return Visibility.Hidden; }
        }
    }
}