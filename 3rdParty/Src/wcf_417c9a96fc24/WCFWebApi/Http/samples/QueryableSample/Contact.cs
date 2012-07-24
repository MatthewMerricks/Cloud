// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace QueryableSample
{
    using System.Globalization;

    public class Contact
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "Name: {0}, ID: {1}", this.Name, this.Id);
        }
    }
}