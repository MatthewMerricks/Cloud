﻿using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace System.Net.Http.Headers
{
    // According to the RFC, in places where a "parameter" is required, the value is mandatory 
    // (e.g. Media-Type, Accept). However, we don't introduce a dedicated type for it. So NameValueHeaderValue supports
    // name-only values in addition to name/value pairs.
    public class NameValueHeaderValue : ICloneable
    {
        private static readonly Func<NameValueHeaderValue> defaultNameValueCreator = CreateNameValue;

        private string name;
        private string value;

        public string Name
        {
            get { return name; }
        }

        public string Value
        {
            get { return value; }
            set
            {
                CheckValueFormat(value);
                this.value = value;
            }
        }

        internal NameValueHeaderValue()
        {
        }

        public NameValueHeaderValue(string name)
            : this(name, null)
        {
        }

        public NameValueHeaderValue(string name, string value)
        {
            CheckNameValueFormat(name, value);

            this.name = name;
            this.value = value;
        }

        protected NameValueHeaderValue(NameValueHeaderValue source)
        {
            Contract.Requires(source != null);

            this.name = source.name;
            this.value = source.value;
        }

        public override int GetHashCode()
        {
            Contract.Assert(name != null);

            int nameHashCode = name.ToLowerInvariant().GetHashCode();

            if (!string.IsNullOrEmpty(value))
            {
                // If we have a quoted-string, then just use the hash code. If we have a token, convert to lowercase 
                // and retrieve the hash code.
                if (value[0] == '"')
                {
                    return nameHashCode ^ value.GetHashCode();
                }

                return nameHashCode ^ value.ToLowerInvariant().GetHashCode();
            }

            return nameHashCode;
        }

        public override bool Equals(object obj)
        {
            NameValueHeaderValue other = obj as NameValueHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (string.Compare(name, other.name, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return false;
            }

            // RFC2616: 14.20: unquoted tokens should use case-INsensitive comparison; quoted-strings should use
            // case-sensitive comparison. The RFC doesn't mention how to compare quoted-strings outside the "Expect"
            // header. We treat all quoted-strings the same: case-sensitive comparison. 

            if (string.IsNullOrEmpty(value))
            {
                return string.IsNullOrEmpty(other.value);
            }

            if (value[0] == '"')
            {
                // We have a quoted string, so we need to do case-sensitive comparison.
                return (string.CompareOrdinal(value, other.value) == 0);
            }
            else
            {
                return (string.Compare(value, other.value, StringComparison.OrdinalIgnoreCase) == 0);
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(value))
            {
                return name + "=" + value;
            }
            return name;
        }

        internal static void ToString(ICollection<NameValueHeaderValue> values, char separator, bool leadingSeparator,
            StringBuilder destination)
        {
            Contract.Assert(destination != null);

            if ((values == null) || (values.Count == 0))
            {
                return;
            }

            foreach (var value in values)
            {
                if (leadingSeparator || (destination.Length > 0))
                {
                    destination.Append(separator);
                    destination.Append(' ');
                }
                destination.Append(value.ToString());
            }
        }

        internal static string ToString(ICollection<NameValueHeaderValue> values, char separator, bool leadingSeparator)
        {
            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();

            ToString(values, separator, leadingSeparator, sb);

            return sb.ToString();
        }

        internal static int GetHashCode(ICollection<NameValueHeaderValue> values)
        {
            if ((values == null) || (values.Count == 0))
            {
                return 0;
            }

            int result = 0;
            foreach (var value in values)
            {
                result = result ^ value.GetHashCode();
            }
            return result;
        }

        internal static int GetNameValueLength(string input, int startIndex, out NameValueHeaderValue parsedValue)
        {
            return GetNameValueLength(input, startIndex, defaultNameValueCreator, out parsedValue);
        }

        internal static int GetNameValueLength(string input, int startIndex,
            Func<NameValueHeaderValue> nameValueCreator, out NameValueHeaderValue parsedValue)
        {
            Contract.Requires(input != null);
            Contract.Requires(startIndex >= 0);
            Contract.Requires(nameValueCreator != null);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Parse the name, i.e. <name> in name/value string "<name>=<value>". Caller must remove 
            // leading whitespaces.
            int nameLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (nameLength == 0)
            {
                return 0;
            }

            string name = input.Substring(startIndex, nameLength);
            int current = startIndex + nameLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the separator between name and value
            if ((current == input.Length) || (input[current] != '='))
            {
                // We only have a name and that's OK. Return.
                parsedValue = nameValueCreator();
                parsedValue.name = name;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
                return current - startIndex;
            }

            current++; // skip delimiter.
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            // Parse the value, i.e. <value> in name/value string "<name>=<value>"
            int valueLength = GetValueLength(input, current);

            if (valueLength == 0)
            {
                return 0; // We have an invalid value. 
            }

            // Use parameterless ctor to avoid double-parsing of name and value, i.e. skip public ctor validation.
            parsedValue = nameValueCreator();
            parsedValue.name = name;
            parsedValue.value = input.Substring(current, valueLength);
            current = current + valueLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current); // skip whitespaces
            return current - startIndex;
        }

        // Returns the length of a name/value list, separated by 'delimiter'. E.g. "a=b, c=d, e=f" adds 3
        // name/value pairs to 'nameValueCollection' if 'delimiter' equals ','.
        internal static int GetNameValueListLength(string input, int startIndex, char delimiter,
            ICollection<NameValueHeaderValue> nameValueCollection)
        {
            Contract.Requires(nameValueCollection != null);
            Contract.Requires(startIndex >= 0);

            if ((string.IsNullOrEmpty(input)) || (startIndex >= input.Length))
            {
                return 0;
            }

            int current = startIndex + HttpRuleParser.GetWhitespaceLength(input, startIndex);
            while (true)
            {
                NameValueHeaderValue parameter = null;
                int nameValueLength = NameValueHeaderValue.GetNameValueLength(input, current,
                    defaultNameValueCreator, out parameter);

                if (nameValueLength == 0)
                {
                    return 0;
                }

                nameValueCollection.Add(parameter);
                current = current + nameValueLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                if ((current == input.Length) || (input[current] != delimiter))
                {
                    // We're done and we have at least one valid name/value pair.
                    return current - startIndex;
                }

                // input[current] is 'delimiter'. Skip the delimiter and whitespaces and try to parse again.
                current++; // skip delimiter.
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }
        }

        internal static NameValueHeaderValue Find(ICollection<NameValueHeaderValue> values, string name)
        {
            Contract.Requires((name != null) && (name.Length > 0));

            if ((values == null) || (values.Count == 0))
            {
                return null;
            }

            foreach (var value in values)
            {
                if (string.Compare(value.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return value;
                }
            }
            return null;
        }

        internal static int GetValueLength(string input, int startIndex)
        {
            Contract.Requires(input != null);

            if (startIndex >= input.Length)
            {
                return 0;
            }

            int valueLength = HttpRuleParser.GetTokenLength(input, startIndex);

            if (valueLength == 0)
            {
                // A value can either be a token or a quoted string. Check if it is a quoted string.
                if (HttpRuleParser.GetQuotedStringLength(input, startIndex, out valueLength) != HttpParseResult.Parsed)
                {
                    // We have an invalid value. Reset the name and return.
                    return 0;
                }
            }
            return valueLength;
        }

        private static void CheckNameValueFormat(string name, string value)
        {
            HeaderUtilities.CheckValidToken(name, "name");
            CheckValueFormat(value);
        }

        private static void CheckValueFormat(string value)
        {
            // Either value is null/empty or a valid token/quoted string
            if (!(string.IsNullOrEmpty(value) || (GetValueLength(value, 0) == value.Length)))
            {
                throw new FormatException(string.Format("The format of value '{0}' is invalid.", value));
            }
        }

        private static NameValueHeaderValue CreateNameValue()
        {
            return new NameValueHeaderValue();
        }

        // Implement ICloneable explicitly to allow derived types to "override" the implementation.
        object ICloneable.Clone()
        {
            return new NameValueHeaderValue(this);
        }
    }
}
