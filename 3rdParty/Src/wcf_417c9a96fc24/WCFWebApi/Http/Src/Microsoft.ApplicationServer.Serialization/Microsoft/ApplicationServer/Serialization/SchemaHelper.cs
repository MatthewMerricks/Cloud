//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ApplicationServer.Serialization
{
    using System;
    using System.Collections;
    using System.Xml.Schema;

    internal static class SchemaHelper
    {
        internal static XmlSchema GetSchema(string ns, XmlSchemaSet schemas)
        {
            if (ns == null) { ns = String.Empty; }

            ICollection currentSchemas = schemas.Schemas();
            foreach (XmlSchema schema in currentSchemas)
            {
                if ((schema.TargetNamespace == null && ns.Length == 0) || ns.Equals(schema.TargetNamespace))
                {
                    return schema;
                }
            }
            return CreateSchema(ns, schemas);
        }

        static XmlSchema CreateSchema(string ns, XmlSchemaSet schemas)
        {
            XmlSchema schema = new XmlSchema();

            schema.ElementFormDefault = XmlSchemaForm.Qualified;
            if (ns.Length > 0)
            {
                schema.TargetNamespace = ns;
                schema.Namespaces.Add(Globals.TnsPrefix, ns);
            }

            schemas.Add(schema);
            return schema;
        }
    }
}


