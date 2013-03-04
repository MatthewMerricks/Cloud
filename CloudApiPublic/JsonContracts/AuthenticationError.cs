//
// AuthenticationError.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using Cloud.Model;
using Cloud.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cloud.JsonContracts
{
    /// <summary>
    /// Contains properties for an authentication error. An array of this error info can be found in <see cref="AuthenticationErrorResponse"/>
    /// </summary>
    [DataContract]
    internal sealed class AuthenticationError
    {
        [DataMember(Name = CLDefinitions.JsonServiceTypeFieldCode, IsRequired = false)]
        public decimal Code
        {
            get
            {
                ulong wholeEnumInteger = (ulong)_codeAsEnum;
                ulong bottomPart = wholeEnumInteger % (((ulong)1) << 32);
                ulong topPart = wholeEnumInteger - bottomPart;
                return decimal.Parse(
                    (topPart >> 32).ToString() + // top part are taken as-is, but need to offset back to the one's place
                    (bottomPart > 0
                        ? "." +
                            string.Join(
                                string.Empty,
                                bottomPart.ToString().Reverse()) // reverse the digits in the integer that represents the part to the right of the decimal ([top part].05 would have had bottom part 50)

                        : string.Empty));
            }
            set
            {
                decimal decimalRemainder = value % 1;
                ulong bottomPart =
                    (decimalRemainder == 0M
                        ? 0
                        : ulong.Parse(
                            string.Join(
                                string.Empty,
                                decimalRemainder.ToString()
                                    .Substring(2) // remove the "0." from the 0.XXX string representation
                                    .Reverse())));
                ulong topPart = ((ulong)(value - decimalRemainder)) << 32; // bit-shift the whole number part of the decimal to the 32 highest-order bits
                _codeAsEnum = (AuthenticationErrorType)(topPart | bottomPart);
            }
        }
        public AuthenticationErrorType CodeAsEnum
        {
            get
            {
                return _codeAsEnum;
            }
            set
            {
                _codeAsEnum = value;
            }
        }
        private AuthenticationErrorType _codeAsEnum = (AuthenticationErrorType)0;

        [DataMember(Name = CLDefinitions.RESTResponseMessage, IsRequired = false)]
        public string Message { get; set; }
    }
}