// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Net.Http.Headers;
using Microsoft.ApplicationServer.Http;

namespace ContactManager_Advanced
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.ServiceModel.Description;

    public class ContactPngFormatter : MediaTypeFormatter
    {
        public ContactPngFormatter()
        {
            this.SupportedMediaTypes.Add(new MediaTypeHeaderValue("image/png"));
        }

        public override void OnWriteToStream(Type type, object value, Stream stream, HttpContentHeaders contentHeaders, System.Net.TransportContext context)
        {
            var contact = value as Contact;
            if (contact != null)
            {
                var imageId = contact.ContactId % 7;
                if (imageId == 0)
                {
                    imageId++;
                }
                
                var path = string.Format(CultureInfo.InvariantCulture, @"{0}bin\Images\Image{1}.png", AppDomain.CurrentDomain.BaseDirectory, (contact.ContactId % 7));
                using (var fileStream = new FileStream(path, FileMode.Open))
                {
                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, (int)fileStream.Length);
                    stream.Write(bytes, 0, (int)fileStream.Length);
                }
            }
        }

        public override object OnReadFromStream(Type type, Stream stream, HttpContentHeaders contentHeaders)
        {
            throw new NotImplementedException();
        }

        protected override bool OnCanReadType(Type type)
        {
            return false;
        }
    }
}
