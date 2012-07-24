// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using Microsoft.ApplicationServer.Common;

    /// <summary>
    /// Class that can convert from values of one type to values of another type based on 
    /// the rules of binding <see cref="HttpParameter"/> instances.
    /// </summary>
    internal abstract class HttpParameterValueConverter
    {
        private static readonly HttpParameterValueConverter booleanValueConverter = new BooleanValueConverter();
        private static readonly HttpParameterValueConverter charValueConverter = new CharValueConverter();
        private static readonly HttpParameterValueConverter signedByteValueConverter = new SByteValueConverter();
        private static readonly HttpParameterValueConverter byteValueConverter = new ByteValueConverter();
        private static readonly HttpParameterValueConverter int16ValueConverter = new Int16ValueConverter();
        private static readonly HttpParameterValueConverter unsignedInt16ValueConverter = new UInt16ValueConverter();
        private static readonly HttpParameterValueConverter int32ValueConverter = new Int32ValueConverter();
        private static readonly HttpParameterValueConverter unsignedInt32ValueConverter = new UInt32ValueConverter();
        private static readonly HttpParameterValueConverter int64ValueConverter = new Int64ValueConverter();
        private static readonly HttpParameterValueConverter unsignedInt64ValueConverter = new UInt64ValueConverter();
        private static readonly HttpParameterValueConverter singleValueConverter = new SingleValueConverter();
        private static readonly HttpParameterValueConverter doubleValueConverter = new DoubleValueConverter();
        private static readonly HttpParameterValueConverter decimalValueConverter = new DecimalValueConverter();
        private static readonly HttpParameterValueConverter dateTimeValueConverter = new DateTimeValueConverter();
        private static readonly HttpParameterValueConverter timeSpanValueConverter = new TimeSpanValueConverter();
        private static readonly HttpParameterValueConverter guidValueConverter = new GuidValueConverter();
        private static readonly HttpParameterValueConverter dateTimeOffsetValueConverter = new DateTimeOffsetValueConverter();
        private static readonly HttpParameterValueConverter uriValueConverter = new UriValueConverter();

        private static readonly Type enumValueConverterGenericType = typeof(EnumValueConverter<>);
        private static readonly Type nonNullableValueConverterGenericType = typeof(NonNullableValueConverter<>);
        private static readonly Type refValueConverterGenericType = typeof(RefValueConverter<>);
        private static readonly Type objectContentValueConverterGenericType = typeof(ObjectContentValueConverter<>);

        protected HttpParameterValueConverter(Type type)
        {
            Fx.Assert(type != null, "The 'type' parameter should not be null.");

            this.Type = type;
        }

        public Type Type { get; private set; }

        public bool CanConvertFromString { get; protected set; }

        public static HttpParameterValueConverter GetValueConverter(Type type)
        {
            if (type == null)
            {
                throw Fx.Exception.ArgumentNull("type");
            }

            Type objectContentTypeArg = HttpTypeHelper.GetHttpContentInnerTypeOrNull(type);

            if (objectContentTypeArg != null)
            {
                Type closedConverterType = objectContentValueConverterGenericType.MakeGenericType(new Type[] { objectContentTypeArg });
                ConstructorInfo constructor = closedConverterType.GetConstructor(Type.EmptyTypes);
                return constructor.Invoke(null) as HttpParameterValueConverter;
            }

            if (HttpTypeHelper.IsHttp(type))
            {
                Type closedConverterType = refValueConverterGenericType.MakeGenericType(new Type[] { type });
                ConstructorInfo constructor = closedConverterType.GetConstructor(Type.EmptyTypes);
                return constructor.Invoke(null) as HttpParameterValueConverter;
            }

            Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);
            bool typeIsNullable = nullableUnderlyingType != null;
            Type actualType = typeIsNullable ? nullableUnderlyingType : type;

            if (actualType.IsEnum)
            {
                Type closedConverterType = enumValueConverterGenericType.MakeGenericType(new Type[] { actualType });
                ConstructorInfo constructor = closedConverterType.GetConstructor(Type.EmptyTypes);
                return constructor.Invoke(null) as HttpParameterValueConverter;
            }

            switch (Type.GetTypeCode(actualType))
            {
                case TypeCode.Boolean: return booleanValueConverter;
                case TypeCode.Char: return charValueConverter;
                case TypeCode.SByte: return signedByteValueConverter;
                case TypeCode.Byte: return byteValueConverter;
                case TypeCode.Int16: return int16ValueConverter;
                case TypeCode.UInt16: return unsignedInt16ValueConverter;
                case TypeCode.Int32: return int32ValueConverter;
                case TypeCode.UInt32: return unsignedInt32ValueConverter;
                case TypeCode.Int64: return int64ValueConverter;                   
                case TypeCode.UInt64: return unsignedInt64ValueConverter;
                case TypeCode.Single: return singleValueConverter;
                case TypeCode.Double: return doubleValueConverter;
                case TypeCode.Decimal: return decimalValueConverter;
                case TypeCode.DateTime: return dateTimeValueConverter;
                default:
                    break;
            }

            if (actualType == typeof(TimeSpan))
            {
                return timeSpanValueConverter;
            }
            else if (actualType == typeof(Guid))
            {
                return guidValueConverter;
            }
            else if (actualType == typeof(DateTimeOffset))
            {
                return dateTimeOffsetValueConverter;
            }
            else if (actualType == typeof(Uri))
            {
                return uriValueConverter;
            }

            Type closedValueConverterType = nonNullableValueConverterGenericType.MakeGenericType(new Type[] { actualType });
            ConstructorInfo valueConverterConstructor = closedValueConverterType.GetConstructor(Type.EmptyTypes);
            return valueConverterConstructor.Invoke(null) as HttpParameterValueConverter;
        }

        public abstract object Convert(object value);

        public abstract bool IsInstanceOf(object value);

        public abstract bool CanConvertFromType(Type type);

        protected virtual object ConvertFromString(string value)
        {
            return null;
        }

        private class RefValueConverter<T> : HttpParameterValueConverter
        {
            public RefValueConverter()
                : base(typeof(T))
            {
            }

            public override object Convert(object value)
            {
                if (value == null)
                {
                    return null;
                }
                else if (value is T)
                {
                    return value;
                }

                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.ValueConversionFailed(value.GetType().FullName, this.Type.FullName)));
            }

            public override bool IsInstanceOf(object value)
            {
                return value == null ?
                    true :
                    value is T;
            }

            public override bool CanConvertFromType(Type type)
            {
                if (type == null)
                {
                    throw Fx.Exception.ArgumentNull("type");
                }

                return this.Type.IsAssignableFrom(type);
            }
        }

        private class ObjectContentValueConverter<T> : HttpParameterValueConverter
        {
            public ObjectContentValueConverter()
                : base(typeof(ObjectContent<T>))
            {
            }

            public override object Convert(object value)
            {
                if (value == null)
                {
                    return null;
                }
                else if (value is ObjectContent<T>)
                {
                    return value;
                }
                else if (value is HttpRequestMessage<T>)
                {
                    return ((HttpRequestMessage<T>)value).Content;
                }
                else if (value is HttpResponseMessage<T>)
                {
                    return ((HttpResponseMessage<T>)value).Content;
                }

                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.ValueConversionFailed(value.GetType().FullName, this.Type.FullName)));
            }

            public override bool IsInstanceOf(object value)
            {
                return value == null ?
                    true :
                    value is ObjectContent<T>;
            }

            public override bool CanConvertFromType(Type type)
            {
                if (type == null)
                {
                    throw Fx.Exception.ArgumentNull("type");
                }

                return this.Type.IsAssignableFrom(type) ||
                       typeof(HttpRequestMessage<T>).IsAssignableFrom(type) ||
                       typeof(HttpResponseMessage<T>).IsAssignableFrom(type);
            }
        }

        private class NonNullableValueConverter<T> : HttpParameterValueConverter
        {
            private object defaultValue;

            public NonNullableValueConverter() 
                : base(typeof(T))
            {
                this.defaultValue = default(T);
            }

            public override object Convert(object value)
            {
                if (value == null)
                {
                    return this.defaultValue;
                }
                else if (value is T)
                {
                    return value;
                }
                else if (this.CanConvertFromString && value is string)
                {
                    string valueAsString = (string)value;
                    if (string.IsNullOrWhiteSpace(valueAsString))
                    {
                        return null;
                    }

                    return this.ConvertFromString((string)value);
                }
                else if (value is HttpRequestMessage<T>)
                {
                    HttpRequestMessage<T> valueAsRequest = (HttpRequestMessage<T>)value;
                    return valueAsRequest.Content.ReadAs();
                }
                else if (value is HttpResponseMessage<T>)
                {
                    HttpResponseMessage<T> valueAsResponse = (HttpResponseMessage<T>)value;
                    return valueAsResponse.Content.ReadAs();
                }
                else if (value is ObjectContent<T>)
                {
                    ObjectContent<T> valueAsContent = (ObjectContent<T>)value;
                    return valueAsContent.ReadAsOrDefault();
                }

                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.ValueConversionFailed(value.GetType().FullName, this.Type.FullName)));
            }

            public override bool IsInstanceOf(object value)
            {
                return value == null ?
                    true :
                    value is T;
            }

            public sealed override bool CanConvertFromType(Type type)
            {
                if (type == null)
                {
                    throw Fx.Exception.ArgumentNull("type");
                }

                if (this.Type.IsAssignableFrom(type)) 
                {
                    return true;
                }

                if (type == TypeHelper.StringType && this.CanConvertFromString)
                {
                    return true;
                }

                if (typeof(HttpRequestMessage<T>).IsAssignableFrom(type) ||
                    typeof(HttpResponseMessage<T>).IsAssignableFrom(type) ||
                    typeof(ObjectContent<T>).IsAssignableFrom(type))
                {
                    return true;
                }

                return false;
            }
        }

        private class ValueConverter<T> : HttpParameterValueConverter where T : struct
        {
            private object defaultValue;

            public ValueConverter()
                : base(typeof(T))
            {
                this.defaultValue = default(T);
            }

            public override object Convert(object value)
            {
                if (value == null)
                {
                    return this.defaultValue;
                }
                else if (value is T)
                {
                    return value;
                }
                else if (this.CanConvertFromString && value is string)
                {
                    string valueAsString = (string)value;
                    if (string.IsNullOrWhiteSpace(valueAsString))
                    {
                        return null;
                    }

                    return this.ConvertFromString((string)value);
                }
                else if (value is HttpRequestMessage<T>)
                {
                    HttpRequestMessage<T> valueAsRequest = (HttpRequestMessage<T>)value;
                    return valueAsRequest.Content.ReadAs();
                }
                else if (value is HttpRequestMessage<T?>)
                {
                    HttpRequestMessage<T?> valueAsRequest = (HttpRequestMessage<T?>)value;
                    return valueAsRequest.Content.ReadAs();
                }
                else if (value is HttpResponseMessage<T>)
                {
                    HttpResponseMessage<T> valueAsResponse = (HttpResponseMessage<T>)value;
                    return valueAsResponse.Content.ReadAs();
                }
                else if (value is HttpResponseMessage<T?>)
                {
                    HttpResponseMessage<T?> valueAsResponse = (HttpResponseMessage<T?>)value;
                    return valueAsResponse.Content.ReadAs();
                }
                else if (value is ObjectContent<T>)
                {
                    ObjectContent<T> valueAsContent = (ObjectContent<T>)value;
                    return valueAsContent.ReadAsOrDefault();
                }
                else if (value is ObjectContent<T?>)
                {
                    ObjectContent<T?> valueAsContent = (ObjectContent<T?>)value;
                    return valueAsContent.ReadAsOrDefault();
                }

                throw Fx.Exception.AsError(
                    new InvalidOperationException(
                        SR.ValueConversionFailed(value.GetType().FullName, this.Type.FullName)));
            }

            public override bool IsInstanceOf(object value)
            {
                return value == null ?
                    true :
                    value is T;
            }

            public sealed override bool CanConvertFromType(Type type)
            {
                if (type == null)
                {
                    throw Fx.Exception.ArgumentNull("type");
                }

                Type underlyingType = Nullable.GetUnderlyingType(type);
                if (underlyingType != null)
                {
                    type = underlyingType;
                }

                if (this.Type.IsAssignableFrom(type))
                {
                    return true;
                }

                if (type == TypeHelper.StringType && this.CanConvertFromString)
                {
                    return true;
                }

                if (typeof(HttpRequestMessage<T>).IsAssignableFrom(type) ||
                    typeof(HttpRequestMessage<T?>).IsAssignableFrom(type) ||
                    typeof(HttpResponseMessage<T>).IsAssignableFrom(type) ||
                    typeof(HttpResponseMessage<T?>).IsAssignableFrom(type) ||
                    typeof(ObjectContent<T>).IsAssignableFrom(type) ||
                    typeof(ObjectContent<T?>).IsAssignableFrom(type))
                {
                    return true;
                }

                return false;
            }
        }

        private class BooleanValueConverter : ValueConverter<bool>
        {
            public BooleanValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return bool.Parse(value);
            }
        }

        private class CharValueConverter : ValueConverter<char>
        {
            public CharValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                Fx.Assert(value.Length > 0, "The 'value' string parameter should not be empty.");
                return value[0];
            }
        }

        private class SByteValueConverter : ValueConverter<sbyte>
        {
            public SByteValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return sbyte.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }
        }
        
        private class ByteValueConverter : ValueConverter<byte>
        {
            public ByteValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return byte.Parse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite, NumberFormatInfo.InvariantInfo);
            }
        }
        
        private class Int16ValueConverter : ValueConverter<short>
        {
            public Int16ValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return short.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }
        }
        
        private class UInt16ValueConverter : ValueConverter<ushort>
        {
            public UInt16ValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return ushort.Parse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite, NumberFormatInfo.InvariantInfo);
            }
        }

        private class Int32ValueConverter : ValueConverter<int>
        {
            public Int32ValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return int.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }
        }

        private class UInt32ValueConverter : ValueConverter<uint>
        {
            public UInt32ValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return uint.Parse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite, NumberFormatInfo.InvariantInfo);
            }
        }

        private class Int64ValueConverter : ValueConverter<long>
        {
            public Int64ValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return long.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }
        }

        private class UInt64ValueConverter : ValueConverter<ulong>
        {
            public UInt64ValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return ulong.Parse(value, NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite, NumberFormatInfo.InvariantInfo);
            }
        }

        private class SingleValueConverter : ValueConverter<float>
        {
            public SingleValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return float.Parse(value, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, NumberFormatInfo.InvariantInfo);
            }
        }

        private class DoubleValueConverter : ValueConverter<double>
        {
            public DoubleValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return double.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
            }
        }

        private class DecimalValueConverter : ValueConverter<decimal>
        {
            public DecimalValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return decimal.Parse(value, NumberStyles.AllowDecimalPoint | NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }
        }

        private class DateTimeValueConverter : ValueConverter<DateTime>
        {
            public DateTimeValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
        }

        private class TimeSpanValueConverter : ValueConverter<TimeSpan>
        {
            public TimeSpanValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return TimeSpan.Parse(value, CultureInfo.InvariantCulture);
            }
        }

        private class GuidValueConverter : ValueConverter<Guid>
        {
            public GuidValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return new Guid(value);
            }
        }

        private class DateTimeOffsetValueConverter : ValueConverter<DateTimeOffset>
        {
            public DateTimeOffsetValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind | DateTimeStyles.AllowWhiteSpaces);
            }
        }

        private class UriValueConverter : NonNullableValueConverter<Uri>
        {
            public UriValueConverter()
            {
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return new Uri(value, UriKind.RelativeOrAbsolute);
            }
        }

        private class EnumValueConverter<T> : ValueConverter<T> where T : struct
        {
            public EnumValueConverter()
            {
                Fx.Assert(this.Type.IsEnum == true, "The EnumValueConverter should only be used with enum types.");
                this.CanConvertFromString = true;
            }

            protected override object ConvertFromString(string value)
            {
                return Enum.Parse(this.Type, value, true);
            }
        }
    }
}
