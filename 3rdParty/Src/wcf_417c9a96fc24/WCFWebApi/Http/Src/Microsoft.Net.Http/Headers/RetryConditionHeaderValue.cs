﻿using System.Diagnostics.Contracts;
using System.Globalization;

namespace System.Net.Http.Headers
{
    public class RetryConditionHeaderValue : ICloneable
    {
        private DateTimeOffset? date;
        private TimeSpan? delta;

        public DateTimeOffset? Date
        {
            get { return date; }
        }

        public TimeSpan? Delta
        {
            get { return delta; }
        }

        public RetryConditionHeaderValue(DateTimeOffset date)
        {
            this.date = date;
        }

        public RetryConditionHeaderValue(TimeSpan delta)
        {
            // The amount of seconds for 'delta' must be in the range 0..2^31
            if (delta.TotalSeconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException("delta");
            }

            this.delta = delta;
        }

        private RetryConditionHeaderValue(RetryConditionHeaderValue source)
        {
            Contract.Requires(source != null);

            this.delta = source.delta;
            this.date = source.date;
        }

        private RetryConditionHeaderValue()
        {
        }

        public override string ToString()
        {
            if (delta.HasValue)
            {
                return ((int)delta.Value.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
            }
            return HttpRuleParser.DateToString(date.Value);
        }

        public override bool Equals(object obj)
        {
            RetryConditionHeaderValue other = obj as RetryConditionHeaderValue;

            if (other == null)
            {
                return false;
            }

            if (delta.HasValue)
            {
                return (other.delta != null) && (delta.Value == other.delta.Value);
            }

            return (other.date != null) && (date.Value == other.date.Value);
        }

        public override int GetHashCode()
        {
            if (delta == null)
            {
                return date.Value.GetHashCode();
            }

            return delta.Value.GetHashCode();
        }

        internal static int GetRetryConditionLength(string input, int startIndex, out object parsedValue)
        {
            Contract.Requires(startIndex >= 0);

            parsedValue = null;

            if (string.IsNullOrEmpty(input) || (startIndex >= input.Length))
            {
                return 0;
            }

            int current = startIndex;

            // Caller must remove leading whitespaces.
            DateTimeOffset date = DateTimeOffset.MinValue;
            int deltaSeconds = -1; // use -1 to indicate that the value was not set. 'delta' values are always >=0

            // We either have a timespan or a date/time value. Determine which one we have by looking at the first char.
            // If it is a number, we have a timespan, otherwise we assume we have a date.
            char firstChar = input[current];

            if ((firstChar >= '0') && (firstChar <= '9'))
            {
                int deltaStartIndex = current;
                int deltaLength = HttpRuleParser.GetNumberLength(input, current, false);

                // The value must be in the range 0..2^31
                if ((deltaLength == 0) || (deltaLength > HttpRuleParser.MaxInt32Digits))
                {
                    return 0;
                }

                current = current + deltaLength;
                current = current + HttpRuleParser.GetWhitespaceLength(input, current);

                // RetryConditionHeaderValue only allows 1 value. There must be no delimiter/other chars after 'delta'
                if (current != input.Length)
                {
                    return 0; 
                }

                if (!HeaderUtilities.TryParseInt32(input.Substring(deltaStartIndex, deltaLength), out deltaSeconds))
                {
                    return 0; // int.TryParse() may return 'false' if the value has 10 digits and is > Int32.MaxValue.
                }
            }
            else
            {
                if (!HttpRuleParser.TryStringToDate(input.Substring(current), out date))
                {
                    return 0;
                }

                // If we got a valid date, then the parser consumed the whole string (incl. trailing whitespaces).
                current = input.Length;
            }

            RetryConditionHeaderValue result = new RetryConditionHeaderValue();

            if (deltaSeconds == -1) // we didn't change delta, so we must have found a date.
            {
                result.date = date;
            }
            else
            {
                result.delta = new TimeSpan(0, 0, deltaSeconds);
            }

            parsedValue = result;
            return current - startIndex;
        }

        object ICloneable.Clone()
        {
            return new RetryConditionHeaderValue(this);
        }

    }
}
