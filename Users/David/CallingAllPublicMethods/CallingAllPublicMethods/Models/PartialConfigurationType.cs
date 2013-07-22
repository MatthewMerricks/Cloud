using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CallingAllPublicMethods.Models
{
    public partial class ConfigurationType
    {
        // overridden to display correctly in a dropdown
        public override string ToString()
        {
            if (string.IsNullOrEmpty(this.nameField))
            {
                return this.idField.ToString();
            }
            return this.nameField;
        }
    }
}