﻿using System.Diagnostics.Contracts;
using System.Text;

namespace System.Net.Http.Headers
{
    public class ViaHeaderValue : ICloneable
    {
        private string protocolName;
        private string protocolVersion;
        private string receivedBy;
        private string comment;

        public string ProtocolName
        {
            get { return protocolName; }
        }

        public string ProtocolVersion
        {
            get { return protocolVersion; }
        }

        public string ReceivedBy
        {
            get { return receivedBy; }
        }

        public string Comment 
        {
            get { return comment; }
        }

        public ViaHeaderValue(string protocolVersion, string receivedBy)
            : this(protocolVersion, receivedBy, null, null)
        {
        }

        public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName)
            : this(protocolVersion, receivedBy, protocolName, null)
        {
        }

        public ViaHeaderValue(string protocolVersion, string receivedBy, string protocolName, string comment)
        {
            HeaderUtilities.CheckValidToken(protocolVersion, "protocolVersion");
            CheckReceivedBy(receivedBy);

            if (!string.IsNullOrEmpty(protocolName))
            {
                HeaderUtilities.CheckValidToken(protocolName, "protocolName");
                this.protocolName = protocolName;
            }

            if (!string.IsNullOrEmpty(comment))
            {
                HeaderUtilities.CheckValidComment(comment, "comment");
                this.comment = comment;
            }

            this.protocolVersion = protocolVersion;
            this.receivedBy = receivedBy;
        }

        private ViaHeaderValue()
        {
        }

        private ViaHeaderValue(ViaHeaderValue source)
        {
            Contract.Requires(source != null);

            this.protocolName = source.protocolName;
            this.protocolVersion = source.protocolVersion;
            this.receivedBy = source.receivedBy;
            this.comment = source.comment;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(protocolName))
            {
                sb.Append(protocolName);
                sb.Append('/');
            }

            sb.Append(protocolVersion);
            sb.Append(' ');
            sb.Append(receivedBy);

            if (!string.IsNullOrEmpty(comment))
            {
                sb.Append(' ');
                sb.Append(comment);
            }

            return sb.ToString();
        }

        public override bool Equals(object obj)
        {
            ViaHeaderValue other = obj as ViaHeaderValue;

            if (other == null)
            {
                return false;
            }

            // Note that for token and host case-insensitive comparison is used. Comments are compared using case-
            // sensitive comparison.
            return (string.Compare(protocolVersion, other.protocolVersion, StringComparison.OrdinalIgnoreCase) == 0) &&
                (string.Compare(receivedBy, other.receivedBy, StringComparison.OrdinalIgnoreCase) == 0) &&
                (string.Compare(protocolName, other.protocolName, StringComparison.OrdinalIgnoreCase) == 0) &&
                (string.CompareOrdinal(comment, other.comment) == 0);
        }

        public override int GetHashCode()
        {
            int result = protocolVersion.ToLowerInvariant().GetHashCode() ^ receivedBy.ToLowerInvariant().GetHashCode();

            if (!string.IsNullOrEmpty(protocolName))
            {
                result = result ^ protocolName.ToLowerInvariant().GetHashCode();
            }

            if (!string.IsNullOrEmpty(comment))
            {
                result = result ^ comment.GetHashCode();
            }

            return result;
        }

        internal static int GetViaLength(string input, int startIndex, out object parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            // Read <protocolName> and <protocolVersion> in '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'
            string protocolName = null;
            string protocolVersion = null;
            int current = GetProtocolEndIndex(input, startIndex, out protocolName, out protocolVersion);

            // If we reached the end of the string after reading protocolName/Version we return (we expect at least
            // <receivedBy> to follow). If reading protocolName/Version read 0 bytes, we return. 
            if ((current == startIndex) || (current == input.Length))
            {
                return 0;
            }
            Contract.Assert(protocolVersion != null);

            // Read <receivedBy> in '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'
            string receivedBy = null;
            int receivedByLength = HttpRuleParser.GetHostLength(input, current, true, out receivedBy);

            if (receivedByLength == 0)
            { 
                return 0;
            }

            current = current + receivedByLength;
            current = current + HttpRuleParser.GetWhitespaceLength(input, current);

            string comment = null;
            if ((current < input.Length) && (input[current] == '('))
            {
                // We have a <comment> in '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'
                int commentLength = 0;
                if (HttpRuleParser.GetCommentLength(input, current, out commentLength) != HttpParseResult.Parsed)
                {
                    return 0; // We found a '(' character but it wasn't a valid comment. Abort.
                }

                comment = input.Substring(current, commentLength);

                current = current + commentLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);
            }

            ViaHeaderValue result = new ViaHeaderValue();
            result.protocolVersion = protocolVersion;
            result.protocolName = protocolName;
            result.receivedBy = receivedBy;
            result.comment = comment;

            parsedValue = result;
            return current - startIndex;
        }

        private static int GetProtocolEndIndex(string input, int startIndex, out string protocolName, 
            out string protocolVersion)
        {
            // We have a string of the form '[<protocolName>/]<protocolVersion> <receivedBy> [<comment>]'. The first
            // token may either be the protocol name or protocol version. We'll only find out after reading the token
            // and by looking at the following character: If it is a '/' we just parsed the protocol name, otherwise
            // the protocol version.
            protocolName = null;
            protocolVersion = null;

            int current = startIndex;
            int protocolVersionOrNameLength = HttpRuleParser.GetTokenLength(input, current);

            if (protocolVersionOrNameLength == 0)
            {
                return 0;
            }

            current = startIndex + protocolVersionOrNameLength;
            int whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
            current = current + whitespaceLength;

            if (current == input.Length)
            {
                return 0;
            }

            if (input[current] == '/')
            {
                // We parsed the protocol name
                protocolName = input.Substring(startIndex, protocolVersionOrNameLength);

                current++; // skip the '/' delimiter
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                protocolVersionOrNameLength = HttpRuleParser.GetTokenLength(input, current);

                if (protocolVersionOrNameLength == 0)
                {
                    return 0; // We have a string "<token>/" followed by non-token chars. This is invalid.
                }

                protocolVersion = input.Substring(current, protocolVersionOrNameLength);

                current = current + protocolVersionOrNameLength;
                whitespaceLength = HttpRuleParser.GetWhitespaceLength(input, current);
                current = current + whitespaceLength;
            }
            else
            {
                protocolVersion = input.Substring(startIndex, protocolVersionOrNameLength);
            }

            if (whitespaceLength == 0)
            {
                return 0; // We were able to parse [<protocolName>/]<protocolVersion> but it wasn't followed by a WS
            }

            return current;
        }

        object ICloneable.Clone()
        {
            return new ViaHeaderValue(this);
        }

        private static void CheckReceivedBy(string receivedBy)
        {
            if (string.IsNullOrEmpty(receivedBy))
            {
                throw new ArgumentException("The value cannot be null or empty.", "receivedBy");
            }

            // 'receivedBy' can either be a host or a token. Since a token is a valid host, we only verify if the value
            // is a valid host.
            string host = null;
            if (HttpRuleParser.GetHostLength(receivedBy, 0, true, out host) != receivedBy.Length)
            {
                throw new FormatException(string.Format("The format of value '{0}' is invalid.", receivedBy));
            }
        }
    }
}
