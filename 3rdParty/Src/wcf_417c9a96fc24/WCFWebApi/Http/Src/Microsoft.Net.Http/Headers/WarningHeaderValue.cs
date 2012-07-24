﻿using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;

namespace System.Net.Http.Headers
{
    public class WarningHeaderValue : ICloneable
    {
        private int code;
        private string agent;
        private string text;
        private DateTimeOffset? date;

        public int Code 
        {
            get { return code; }
        }

        public string Agent 
        {
            get { return agent; }
        }

        public string Text 
        {
            get { return text; }
        }

        public DateTimeOffset? Date 
        {
            get { return date; }
        }

        public WarningHeaderValue(int code, string agent, string text)
        {
            CheckCode(code);
            CheckAgent(agent);
            HeaderUtilities.CheckValidQuotedString(text, "text");

            this.code = code;
            this.agent = agent;
            this.text = text;
        }

        public WarningHeaderValue(int code, string agent, string text, DateTimeOffset date)
        {
            CheckCode(code);
            CheckAgent(agent);
            HeaderUtilities.CheckValidQuotedString(text, "text");

            this.code = code;
            this.agent = agent;
            this.text = text;
            this.date = date;
        }

        private WarningHeaderValue()
        {
        }

        private WarningHeaderValue(WarningHeaderValue source)
        {
            Contract.Requires(source != null);

            this.code = source.code;
            this.agent = source.agent;
            this.text = source.text;
            this.date = source.date;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // Warning codes are always 3 digits according to RFC2616
            sb.Append(code.ToString("000", NumberFormatInfo.InvariantInfo));

            sb.Append(' ');
            sb.Append(agent);
            sb.Append(' ');
            sb.Append(text);

            if (date.HasValue)
            {
                sb.Append(" \"");
                sb.Append(HttpRuleParser.DateToString(date.Value));
                sb.Append('\"');
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            WarningHeaderValue other = obj as WarningHeaderValue;

            if (other == null)
            {
                return false;
            }

            // 'agent' is a host/token, i.e. use case-insensitive comparison. Use case-sensitive comparison for 'text'
            // since it is a quoted string.
            if ((code != other.code) || (string.Compare(agent, other.agent, StringComparison.OrdinalIgnoreCase) != 0) ||
                (string.CompareOrdinal(text, other.text) != 0))
            {
                return false;            
            }

            // We have a date set. Verify 'other' has also a date that matches our value.
            if (date.HasValue)
            {
                return other.date.HasValue && (date.Value == other.date.Value);
            }

            // We don't have a date. If 'other' has a date, we're not equal.
            return !other.date.HasValue;
        }

        public override int GetHashCode()
        {
            int result = code.GetHashCode() ^ agent.ToLowerInvariant().GetHashCode() ^ text.GetHashCode();

            if (date.HasValue)
            {
                result = result ^ date.Value.GetHashCode();
            }

            return result;
        }

        internal static int GetWarningLength(string input, int startIndex, out object parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Read <code> in '<code> <agent> <text> ["<date>"]'
            int code;
            int current = startIndex;

            if (!TryReadCode(input, ref current, out code))
            {
                return 0;
            }

            // Read <agent> in '<code> <agent> <text> ["<date>"]'
            string agent;
            if (!TryReadAgent(input, current, ref current, out agent))
            {
                return 0;
            }

            // Read <text> in '<code> <agent> <text> ["<date>"]'
            int textLength = 0;
            int textStartIndex = current;
            if (HttpRuleParser.GetQuotedStringLength(input, current, out textLength) != HttpParseResult.Parsed)
            {
                return 0;
            }

            current = current + textLength;

            // Read <date> in '<code> <agent> <text> ["<date>"]'
            DateTimeOffset? date = null;
            if (!TryReadDate(input, ref current, out date))
            {
                return 0;
            }

            WarningHeaderValue result = new WarningHeaderValue();
            result.code = code;
            result.agent = agent;
            result.text = input.Substring(textStartIndex, textLength);
            result.date = date;

            parsedValue = result;
            return current - startIndex;
        }

        private static bool TryReadAgent(string input, int startIndex, ref int current, out string agent)
        {
            agent = null;

            int agentLength = HttpRuleParser.GetHostLength(input, startIndex, true, out agent);

            if (agentLength == 0)
            {
                return false;
            }

            current = current + agentLength;
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            // At least one whitespace required after <agent>. Also make sure we have characters left for <text>
            if ((whitespaceLength == 0) || (current == input.Length))
            {
                return false;
            }

            return true;
        }

        private static bool TryReadCode(string input, ref int current, out int code)
        {
            code = 0;
            int codeLength = HttpRuleParser.GetNumberLength(input, current, false);

            // code must be a 3 digit value. We accept less digits, but we don't accept more.
            if ((codeLength == 0) || (codeLength > 3))
            {
                return false;
            }

            if (!HeaderUtilities.TryParseInt32(input.Substring(current, codeLength), out code))
            {
                Contract.Assert(false, "Unable to parse value even though it was parsed as <=3 digits string. Input: '" +
                    input + "', Current: " + current + ", CodeLength: " + codeLength);
                return false;
            }

            current = current + codeLength;

            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            // Make sure the number is followed by at least one whitespace and that we have characters left to parse.
            if ((whitespaceLength == 0) || (current == input.Length))
            {
                return false;
            }

            return true;
        }

        private static bool TryReadDate(string input, ref int current, out DateTimeOffset? date)
        {
            date = null;

            // Make sure we have at least one whitespace between <text> and <date> (if we have <date>)
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            // Read <date> in '<code> <agent> <text> ["<date>"]'
            if ((current < input.Length) && (input[current] == '"'))
            {
                if (whitespaceLength == 0)
                {
                    return false; // we have characters after <text> but they were not separated by a whitespace
                }

                current++; // skip opening '"'

                // Find the closing '"'
                int dateStartIndex = current;
                while (current < input.Length)
                {
                    if (input[current] == '"')
                    {
                        break;
                    }
                    current++;
                }

                if ((current == input.Length) || (current == dateStartIndex))
                {
                    return false; // we couldn't find the closing '"' or we have an empty quoted string.
                }

                DateTimeOffset temp;
                if (!HttpRuleParser.TryStringToDate(input.Substring(dateStartIndex, current - dateStartIndex), out temp))
                {
                    return false;
                }

                date = temp;

                current++; // skip closing '"'
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }

            return true;
        }

        object ICloneable.Clone()
        {
            return new WarningHeaderValue(this);
        }

        private static void CheckCode(int code)
        {
            if ((code < 0) || (code > 999))
            {
                throw new ArgumentOutOfRangeException("code");
            }
        }

        private static void CheckAgent(string agent)
        {
            if (string.IsNullOrEmpty(agent))
            {
                throw new ArgumentException("The value cannot be null or empty.", "agent");
            }

            // 'receivedBy' can either be a host or a token. Since a token is a valid host, we only verify if the value
            // is a valid host.
            string host = null;
            if (HttpRuleParser.GetHostLength(agent, 0, true, out host) != agent.Length)
            {
                throw new FormatException(string.Format("The format of value '{0}' is invalid.", agent));
            }
        }
    }
}
