// <copyright file="JsonObject.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.Json
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Class which contains many extension methods to do validation on <see cref="System.Json.JsonObject"/> instances.
    /// </summary>
    public static class JsonObjectValidation
    {
        /// <summary>
        /// Validates that this object contains a member with the given name.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidatePresence(this JsonObject value, string key)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            if (value.ContainsKey(key))
            {
                return value;
            }
            else
            {
                ValidationResult failedResult = new ValidationResult(SG.GetString(SR.NamedValueNotPresent, key), new List<string> { key });
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ValidationException(failedResult, null, null));
            }
        }

        /// <summary>
        /// Validates that this object contains a member with the given name, and the value of the member, read as
        /// <see cref="System.String"/>, matches the given regular expression pattern.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="pattern">The regular expression pattern.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateRegularExpression(this JsonObject value, string key, string pattern)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            value.ValidatePresence(key);

            string jsonValue = value[key].ReadAs<string>();

            ValidationContext context = new ValidationContext(value, null, null);
            context.MemberName = key;
            Validator.ValidateValue(jsonValue, context, new List<ValidationAttribute> { new RegularExpressionAttribute(pattern) });
            return value;
        }

        /// <summary>
        /// Validates that this object contains a member with the given name, and the value of the member can be
        /// converted into the given enum type.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="enumType">The enum type to validate the value.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateEnum(this JsonObject value, string key, Type enumType)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            if (enumType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("enumType");
            }

            value.ValidatePresence(key);

            string jsonValue = value[key].ReadAs<string>();

            ValidationContext context = new ValidationContext(value, null, null);
            context.MemberName = key;
            Validator.ValidateValue(jsonValue, context, new List<ValidationAttribute> { new EnumDataTypeAttribute(enumType) });
            return value;
        }

        /// <summary>
        /// Validates that this object contains a member with the given name, and the value of the member, read as
        /// <see cref="System.String"/>, has a length not greater than the given value.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="maximumLength">The maximum length allowed for the member value.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateStringLength(this JsonObject value, string key, int maximumLength)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            return value.ValidateStringLength(key, 0, maximumLength);
        }

        /// <summary>
        /// Validates that this object contains a member with the given name, and the value of the member, read as
        /// <see cref="System.String"/>, has a length within the given range.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="minimumLength">The minimum length allowed for the member value.</param>
        /// <param name="maximumLength">The maximum length allowed for the member value.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateStringLength(this JsonObject value, string key, int minimumLength, int maximumLength)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            value.ValidatePresence(key);

            string jsonValue = value[key].ReadAs<string>();

            ValidationContext context = new ValidationContext(value, null, null);
            context.MemberName = key;
            Validator.ValidateValue(jsonValue, context, new List<ValidationAttribute> { new StringLengthAttribute(maximumLength) { MinimumLength = minimumLength } });
            return value;
        }

        /// <summary>
        /// Validates that the member with the given name can be read as the given type.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <typeparam name="T">The type to validate.</typeparam>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004",
            Justification = "The validation support will be changed (189014); will disable this for now")]
        public static JsonObject ValidateTypeOf<T>(this JsonObject value, string key)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }
            
            value.ValidatePresence(key);

            T tempOfT;
            if (!value[key].TryReadAs<T>(out tempOfT))
            {
                ValidationResult failedResult = new ValidationResult(SG.GetString(SR.NamedValueNotOfType, key, typeof(T).FullName), new List<string> { key });
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ValidationException(failedResult, null, null));
            }

            return value;
        }

        /// <summary>
        /// Validates this object with a custom method.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The name of the key to be passed to the validation context.</param>
        /// <param name="type">Tye type where the custom method is located.</param>
        /// <param name="method">The name of the method used to perform the validation.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateCustomValidator(this JsonObject value, string key, Type type, string method)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }
            
            value.ValidatePresence(key);

            ValidationContext context = new ValidationContext(value, null, null);
            context.MemberName = key;
            List<ValidationAttribute> attrib = new List<ValidationAttribute> { new CustomValidationAttribute(type, method) };
            Validator.ValidateValue(value[key], context, attrib);
            return value;
        }

        /// <summary>
        /// Validates that the member with the given name can be read as a <see cref="System.Int32"/>, and when read as such
        /// it is within the given range.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="min">The lower bound of the range check.</param>
        /// <param name="max">The upper bound of the range check.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateRange(this JsonObject value, string key, int min, int max)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            value.ValidateTypeOf<int>(key);
            return value.ValidateRange<int>(key, value[key].ReadAs<int>(), new RangeAttribute(min, max));
        }

        /// <summary>
        /// Validates that the member with the given name can be read as a <see cref="System.Double"/>, and when read as such
        /// it is within the given range.
        /// </summary>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="min">The lower bound of the range check.</param>
        /// <param name="max">The upper bound of the range check.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateRange(this JsonObject value, string key, double min, double max)
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            value.ValidateTypeOf<double>(key);
            return value.ValidateRange<double>(key, value[key].ReadAs<double>(), new RangeAttribute(min, max));
        }

        /// <summary>
        /// Validates that the member with the given name can be read as the given type, and when read as such
        /// it is within the given range.
        /// </summary>
        /// <typeparam name="T">The type of the member.</typeparam>
        /// <param name="value">The <see cref="System.Json.JsonObject"/> to which the validation will be applied.</param>
        /// <param name="key">The key of the member to search.</param>
        /// <param name="min">The lower bound of the range check.</param>
        /// <param name="max">The upper bound of the range check.</param>
        /// <returns>This object, so that other validation operations can be chained.</returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">If the validation failed.</exception>
        public static JsonObject ValidateRange<T>(this JsonObject value, string key, T min, T max) where T : IComparable
        {
            if (value == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
            }

            if (min == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("min");
            }

            if (max == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("max");
            }

            value.ValidateTypeOf<T>(key);
            return value.ValidateRange<T>(key, value[key].ReadAs<T>(), new RangeAttribute(typeof(T), min.ToString(), max.ToString()));
        }

        private static JsonObject ValidateRange<T>(this JsonObject value, string key, T valueOfT, RangeAttribute rangeAttribute) where T : IComparable
        {
            ValidationContext context = new ValidationContext(value, null, null);
            context.MemberName = key;
            Validator.ValidateValue(valueOfT, context, new List<ValidationAttribute> { rangeAttribute });
            return value;
        }
    }
}

