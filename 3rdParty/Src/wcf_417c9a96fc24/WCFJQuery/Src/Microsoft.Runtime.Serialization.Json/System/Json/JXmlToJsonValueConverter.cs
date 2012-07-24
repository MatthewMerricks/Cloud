// <copyright file="JXmlToJsonValueConverter.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.Json
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;

    internal static class JXmlToJsonValueConverter
    {
        internal const string RootElementName = "root";
        internal const string ItemElementName = "item";
        internal const string TypeAttributeName = "type";
        internal const string ArrayAttributeValue = "array";
        internal const string BooleanAttributeValue = "boolean";
        internal const string NullAttributeValue = "null";
        internal const string NumberAttributeValue = "number";
        internal const string ObjectAttributeValue = "object";
        internal const string StringAttributeValue = "string";
        private const string TypeHintAttributeName = "__type";

        private static readonly char[] FloatingPointChars = new char[] { '.', 'e', 'E' };

        public static void JsonValueToJXML(XmlDictionaryWriter jsonWriter, JsonValue jsonValue)
        {
            if (jsonWriter == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("jsonWriter");
            }

            jsonWriter.WriteStartElement(RootElementName);
            if (jsonValue == null)
            {
                jsonWriter.WriteAttributeString(TypeAttributeName, NullAttributeValue);
            }
            else
            {
                jsonValue.Save(jsonWriter);
            }

            jsonWriter.WriteEndElement();
        }

        public static JsonValue JXMLToJsonValue(Stream jsonStream)
        {
            if (jsonStream == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("jsonStream");
            }

            return JXMLToJsonValue(jsonStream, null);
        }

        public static JsonValue JXMLToJsonValue(string jsonString)
        {
            if (jsonString == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("jsonString");
            }

            if (jsonString.Length == 0)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                    new ArgumentException(SG.GetString(SR.JsonStringCannotBeEmpty), "jsonString"));
            }

            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);

            return JXMLToJsonValue(null, jsonBytes);
        }

        public static JsonValue JXMLToJsonValue(XmlDictionaryReader jsonReader)
        {
            if (jsonReader == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("jsonReader");
            }

            const string RootObjectName = "RootObject";
            Stack<JsonValue> jsonStack = new Stack<JsonValue>();
            string nodeType = null;
            bool isEmptyElement = false;
            JsonValue parent = new JsonObject();
            jsonStack.Push(parent);
            string currentName = RootObjectName;

            try
            {
                MoveToRootNode(jsonReader);
                while (jsonStack.Count > 0 && jsonReader.NodeType != XmlNodeType.None)
                {
                    if (parent is JsonObject && currentName == null)
                    {
                        currentName = GetMemberName(jsonReader);
                    }

                    nodeType = jsonReader.GetAttribute(TypeAttributeName) ?? StringAttributeValue;

                    if (parent is JsonArray)
                    {
                        // For arrays, the element name has to be "item"
                        if (jsonReader.Name != ItemElementName)
                        {
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat)));
                        }
                    }

                    switch (nodeType)
                    {
                        case NullAttributeValue:
                        case BooleanAttributeValue:
                        case StringAttributeValue:
                        case NumberAttributeValue:
                            JsonPrimitive jsonPrimitive = ReadPrimitive(nodeType, jsonReader);
                            InsertJsonValue(jsonStack, ref parent, ref currentName, jsonPrimitive, true);
                            break;
                        case ArrayAttributeValue:
                            JsonArray jsonArray = CreateJsonArray(jsonReader, ref isEmptyElement);
                            InsertJsonValue(jsonStack, ref parent, ref currentName, jsonArray, isEmptyElement);
                            break;
                        case ObjectAttributeValue:
                            JsonObject jsonObject = CreateObjectWithTypeHint(jsonReader, ref isEmptyElement);
                            InsertJsonValue(jsonStack, ref parent, ref currentName, jsonObject, isEmptyElement);
                            break;
                        default:
                            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat)));
                    }

                    while (jsonReader.NodeType == XmlNodeType.EndElement && jsonStack.Count > 0)
                    {
                        jsonReader.Read();
                        SkipWhitespace(jsonReader);
                        jsonStack.Pop();
                        if (jsonStack.Count > 0)
                        {
                            parent = jsonStack.Peek();
                        }
                    }
                }
            }
            catch (XmlException xmlException)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat), xmlException));
            }

            if (jsonStack.Count != 1)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat)));
            }

            return parent[RootObjectName];
        }

        private static JsonValue JXMLToJsonValue(Stream jsonStream, byte[] jsonBytes)
        {
            try
            {
                using (XmlDictionaryReader jsonReader = jsonStream != null ?
                    JsonReaderWriterFactory.CreateJsonReader(jsonStream, XmlDictionaryReaderQuotas.Max) :
                    JsonReaderWriterFactory.CreateJsonReader(jsonBytes, XmlDictionaryReaderQuotas.Max))
                {
                    return JXMLToJsonValue(jsonReader);
                }
            }
            catch (XmlException)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat)));
            }
        }

        private static void InsertJsonValue(Stack<JsonValue> jsonStack, ref JsonValue parent, ref string currentName, JsonValue jsonValue, bool isEmptyElement)
        {
            if (parent is JsonArray)
            {
                ((JsonArray)parent).Add(jsonValue);
            }
            else
            {
                if (currentName != null)
                {
                    ((JsonObject)parent)[currentName] = jsonValue;
                    currentName = null;
                }
            }

            if (!isEmptyElement)
            {
                jsonStack.Push(jsonValue);
                parent = jsonValue;
            }
        }

        private static string GetMemberName(XmlDictionaryReader jsonReader)
        {
            string name;
            if (jsonReader.NamespaceURI == ItemElementName && jsonReader.LocalName == ItemElementName)
            {
                // JXML special case for names which aren't valid XML names
                name = jsonReader.GetAttribute(ItemElementName);

                if (name == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat)));
                }
            }
            else
            {
                name = jsonReader.Name;
            }

            return name;
        }

        private static JsonObject CreateObjectWithTypeHint(XmlDictionaryReader jsonReader, ref bool isEmptyElement)
        {
            JsonObject jsonObject = new JsonObject();
            string typeHintAttribute = jsonReader.GetAttribute(TypeHintAttributeName);
            isEmptyElement = jsonReader.IsEmptyElement;
            jsonReader.ReadStartElement();
            SkipWhitespace(jsonReader);
            if (typeHintAttribute != null)
            {
                jsonObject.Add(TypeHintAttributeName, typeHintAttribute);
            }

            return jsonObject;
        }

        private static JsonArray CreateJsonArray(XmlDictionaryReader jsonReader, ref bool isEmptyElement)
        {
            JsonArray jsonArray = new JsonArray();
            isEmptyElement = jsonReader.IsEmptyElement;
            jsonReader.ReadStartElement();
            SkipWhitespace(jsonReader);
            return jsonArray;
        }

        private static void MoveToRootNode(XmlDictionaryReader jsonReader)
        {
            while (!jsonReader.EOF && (jsonReader.NodeType == XmlNodeType.None || jsonReader.NodeType == XmlNodeType.XmlDeclaration))
            {
                // read into <root> node
                jsonReader.Read();
                SkipWhitespace(jsonReader);
            }

            if (jsonReader.NodeType != XmlNodeType.Element || !string.IsNullOrEmpty(jsonReader.NamespaceURI) || jsonReader.Name != RootElementName)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new FormatException(SG.GetString(SR.IncorrectJsonFormat)));
            }
        }

        private static JsonPrimitive ReadPrimitive(string type, XmlDictionaryReader jsonReader)
        {
            JsonValue result = null;
            switch (type)
            {
                case NullAttributeValue:
                    jsonReader.Skip();
                    result = null;
                    break;
                case BooleanAttributeValue:
                    result = jsonReader.ReadElementContentAsBoolean();
                    break;
                case StringAttributeValue:
                    result = jsonReader.ReadElementContentAsString();
                    break;
                case NumberAttributeValue:
                    string temp = jsonReader.ReadElementContentAsString();
                    result = ConvertStringToJsonNumber(temp);
                    break;
            }

            SkipWhitespace(jsonReader);
            return (JsonPrimitive)result;
        }

        private static void SkipWhitespace(XmlDictionaryReader reader)
        {
            while (!reader.EOF && (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace))
            {
                reader.Read();
            }
        }

        private static JsonValue ConvertStringToJsonNumber(string value)
        {
            if (value.IndexOfAny(FloatingPointChars) < 0)
            {
                int intVal;
                if (int.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out intVal))
                {
                    return intVal;
                }

                long longVal;
                if (long.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out longVal))
                {
                    return longVal;
                }
            }

            decimal decValue;
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decValue) && decValue != 0)
            {
                return decValue;
            }

            double dblValue;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out dblValue))
            {
                return dblValue;
            }

            throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentException(SG.GetString(SR.InvalidJsonPrimitive, value)));
        }
    }
}