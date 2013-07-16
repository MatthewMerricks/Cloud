using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CallingAllPublicMethods.Models
{
    public sealed class DesignDependencyObject : DependencyObject
    {
        private static readonly DesignDependencyObject _instance = new DesignDependencyObject();

        private DesignDependencyObject() { }

        public static bool IsInDesignTool
        {
            get
            {
                return DesignerProperties.GetIsInDesignMode(_instance);
            }
        }
    }
}