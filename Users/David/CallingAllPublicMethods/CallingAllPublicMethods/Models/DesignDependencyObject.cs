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

        /// <summary>
        /// Sets the value of the System.ComponentModel.DesignerProperties.IsInDesignMode attached
        /// property to the <see cref="CallingAllPublicMethods.Models.DesignDependencyObject"/>.
        /// </summary>
        /// <param name="value">The needed System.Boolean value.</param>
        public static void SetIsInDesignMode(bool value)
        {
            DesignerProperties.SetIsInDesignMode(_instance, value);
        }
    }
}