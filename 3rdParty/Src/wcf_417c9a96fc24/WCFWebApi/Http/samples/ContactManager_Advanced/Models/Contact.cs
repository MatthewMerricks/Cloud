// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace ContactManager_Advanced
{
    using System.Data;
    using System.Globalization;

    public class Contact
    {
        public int ContactId { get; set; }

        public string Name { get; set; }

        public string Address { get; set; }
        
        public string City { get; set; }
        
        public string State { get; set; }
        
        public string Zip { get; set; }
        
        public string Email { get; set; }
        
        public string Twitter { get; set; }

        public string Self
        {
            get { return string.Format(CultureInfo.CurrentCulture, "contact/{0}", this.ContactId); }
            set { }
        }
    }
}