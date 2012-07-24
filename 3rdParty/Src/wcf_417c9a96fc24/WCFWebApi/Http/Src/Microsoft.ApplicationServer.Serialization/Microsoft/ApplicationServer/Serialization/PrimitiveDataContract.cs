//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.ApplicationServer.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Security;
    using System.Xml;
    using Microsoft.ApplicationServer.Common;

    internal abstract class PrimitiveDataContract : DataContract
    {
        [Fx.Tag.SecurityNote(Critical = "Holds instance of CriticalHelper which keeps state that is cached statically for serialization."
            + " Static fields are marked SecurityCritical or readonly to prevent data from being modified or leaked to other components in appdomain.")]
        [SecurityCritical]
        PrimitiveDataContractCriticalHelper helper;

        [Fx.Tag.SecurityNote(Critical = "Initializes SecurityCritical field 'helper'.",
            Safe = "Doesn't leak anything.")]
        [SecuritySafeCritical]
        protected PrimitiveDataContract(Type type, XmlDictionaryString name, XmlDictionaryString ns)
            : base(new PrimitiveDataContractCriticalHelper(type, name, ns))
        {
            helper = base.Helper as PrimitiveDataContractCriticalHelper;
        }

        static internal PrimitiveDataContract GetPrimitiveDataContract(Type type)
        {
            return DataContract.GetBuiltInDataContract(type) as PrimitiveDataContract;
        }

        static internal PrimitiveDataContract GetPrimitiveDataContract(string name, string ns)
        {
            return DataContract.GetBuiltInDataContract(name, ns) as PrimitiveDataContract;
        }

        internal override XmlDictionaryString TopLevelElementNamespace
        {
            get { return DictionaryGlobals.SerializationNamespace; }
            set { }
        }

        internal override bool CanContainReferences
        {
            get { return false; }
        }

        internal override bool IsPrimitive
        {
            get { return true; }
        }

        internal override bool IsBuiltInDataContract
        {
            get
            {
                return true;
            }
        }

        internal override bool Equals(object other, Dictionary<DataContractPairKey, object> checkedContracts)
        {
            PrimitiveDataContract dataContract = other as PrimitiveDataContract;
            if (dataContract != null)
            {
                Type thisType = this.GetType();
                Type otherType = other.GetType();
                return (thisType.Equals(otherType) || thisType.IsSubclassOf(otherType) || otherType.IsSubclassOf(thisType));
            }
            return false;
        }

        [Fx.Tag.SecurityNote(Critical = "Holds all state used for for (de)serializing primitives."
            + " Since the data is cached statically, we lock down access to it.")]
        [SecurityCritical]
        class PrimitiveDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            internal PrimitiveDataContractCriticalHelper(Type type, XmlDictionaryString name, XmlDictionaryString ns)
                : base(type)
            {
                SetDataContractName(name, ns);
            }
        }
    }

    internal class CharDataContract : PrimitiveDataContract
    {
        internal CharDataContract()
            : this(DictionaryGlobals.CharLocalName, DictionaryGlobals.SerializationNamespace)
        {
        }

        internal CharDataContract(XmlDictionaryString name, XmlDictionaryString ns)
            : base(typeof(char), name, ns)
        {
        }
    }

    internal class AsmxCharDataContract : CharDataContract
    {
        internal AsmxCharDataContract() 
            : base(DictionaryGlobals.CharLocalName, DictionaryGlobals.AsmxTypesNamespace) 
        {
        }
    }

    internal class BooleanDataContract : PrimitiveDataContract
    {
        internal BooleanDataContract()
            : base(typeof(bool), DictionaryGlobals.BooleanLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class SignedByteDataContract : PrimitiveDataContract
    {
        internal SignedByteDataContract()
            : base(typeof(sbyte), DictionaryGlobals.SignedByteLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class UnsignedByteDataContract : PrimitiveDataContract
    {
        internal UnsignedByteDataContract()
            : base(typeof(byte), DictionaryGlobals.UnsignedByteLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class ShortDataContract : PrimitiveDataContract
    {
        internal ShortDataContract()
            : base(typeof(short), DictionaryGlobals.ShortLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class UnsignedShortDataContract : PrimitiveDataContract
    {
        internal UnsignedShortDataContract()
            : base(typeof(ushort), DictionaryGlobals.UnsignedShortLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class IntDataContract : PrimitiveDataContract
    {
        internal IntDataContract()
            : base(typeof(int), DictionaryGlobals.IntLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class UnsignedIntDataContract : PrimitiveDataContract
    {
        internal UnsignedIntDataContract()
            : base(typeof(uint), DictionaryGlobals.UnsignedIntLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class LongDataContract : PrimitiveDataContract
    {
        internal LongDataContract()
            : this(DictionaryGlobals.LongLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal LongDataContract(XmlDictionaryString name, XmlDictionaryString ns)
            : base(typeof(long), name, ns)
        {
        }
    }
    internal class IntegerDataContract : LongDataContract
    {
        internal IntegerDataContract() 
            : base(DictionaryGlobals.integerLocalName, DictionaryGlobals.SchemaNamespace)
        { 
        }
    }
    internal class PositiveIntegerDataContract : LongDataContract
    {
        internal PositiveIntegerDataContract() 
            : base(DictionaryGlobals.positiveIntegerLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }
    internal class NegativeIntegerDataContract : LongDataContract
    {
        internal NegativeIntegerDataContract() 
            : base(DictionaryGlobals.negativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }
    internal class NonPositiveIntegerDataContract : LongDataContract
    {
        internal NonPositiveIntegerDataContract() 
            : base(DictionaryGlobals.nonPositiveIntegerLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }
    internal class NonNegativeIntegerDataContract : LongDataContract
    {
        internal NonNegativeIntegerDataContract() 
            : base(DictionaryGlobals.nonNegativeIntegerLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class UnsignedLongDataContract : PrimitiveDataContract
    {
        internal UnsignedLongDataContract()
            : base(typeof(ulong), DictionaryGlobals.UnsignedLongLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class FloatDataContract : PrimitiveDataContract
    {
        internal FloatDataContract()
            : base(typeof(float), DictionaryGlobals.FloatLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class DoubleDataContract : PrimitiveDataContract
    {
        internal DoubleDataContract()
            : base(typeof(double), DictionaryGlobals.DoubleLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class DecimalDataContract : PrimitiveDataContract
    {
        internal DecimalDataContract()
            : base(typeof(decimal), DictionaryGlobals.DecimalLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class DateTimeDataContract : PrimitiveDataContract
    {
        internal DateTimeDataContract()
            : base(typeof(DateTime), DictionaryGlobals.DateTimeLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class StringDataContract : PrimitiveDataContract
    {
        internal StringDataContract()
            : this(DictionaryGlobals.StringLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal StringDataContract(XmlDictionaryString name, XmlDictionaryString ns)
            : base(typeof(string), name, ns)
        {
        }
    }
    internal class TimeDataContract : StringDataContract
    {
        internal TimeDataContract() 
            : base(DictionaryGlobals.timeLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class DateDataContract : StringDataContract
    {
        internal DateDataContract() 
            : base(DictionaryGlobals.dateLocalName, DictionaryGlobals.SchemaNamespace) 
        { 
        }
    }

    internal class HexBinaryDataContract : StringDataContract
    {
        internal HexBinaryDataContract()
            : base(DictionaryGlobals.hexBinaryLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class GYearMonthDataContract : StringDataContract
    {
        internal GYearMonthDataContract() 
            : base(DictionaryGlobals.gYearMonthLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class GYearDataContract : StringDataContract
    {
        internal GYearDataContract() 
            : base(DictionaryGlobals.gYearLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class GMonthDayDataContract : StringDataContract
    {
        internal GMonthDayDataContract() 
            : base(DictionaryGlobals.gMonthDayLocalName, DictionaryGlobals.SchemaNamespace) 
        { 
        }
    }

    internal class GDayDataContract : StringDataContract
    {
        internal GDayDataContract() 
            : base(DictionaryGlobals.gDayLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class GMonthDataContract : StringDataContract
    {
        internal GMonthDataContract() 
            : base(DictionaryGlobals.gMonthLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class NormalizedStringDataContract : StringDataContract
    {
        internal NormalizedStringDataContract() 
            : base(DictionaryGlobals.normalizedStringLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class TokenDataContract : StringDataContract
    {
        internal TokenDataContract() 
            : base(DictionaryGlobals.tokenLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class LanguageDataContract : StringDataContract
    {
        internal LanguageDataContract() 
            : base(DictionaryGlobals.languageLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class NameDataContract : StringDataContract
    {
        internal NameDataContract() 
            : base(DictionaryGlobals.NameLocalName, DictionaryGlobals.SchemaNamespace)
        { 
        }
    }

    internal class NCNameDataContract : StringDataContract
    {
        internal NCNameDataContract() : base(DictionaryGlobals.NCNameLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class IDDataContract : StringDataContract
    {
        internal IDDataContract()
            : base(DictionaryGlobals.XSDIDLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class IDREFDataContract : StringDataContract
    {
        internal IDREFDataContract() 
            : base(DictionaryGlobals.IDREFLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class IDREFSDataContract : StringDataContract
    {
        internal IDREFSDataContract() 
            : base(DictionaryGlobals.IDREFSLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class ENTITYDataContract : StringDataContract
    {
        internal ENTITYDataContract() 
            : base(DictionaryGlobals.ENTITYLocalName, DictionaryGlobals.SchemaNamespace) 
        { 
        }
    }

    internal class ENTITIESDataContract : StringDataContract
    {
        internal ENTITIESDataContract() 
            : base(DictionaryGlobals.ENTITIESLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class NMTOKENDataContract : StringDataContract
    {
        internal NMTOKENDataContract() 
            : base(DictionaryGlobals.NMTOKENLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class NMTOKENSDataContract : StringDataContract
    {
        internal NMTOKENSDataContract() 
            : base(DictionaryGlobals.NMTOKENSLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class ByteArrayDataContract : PrimitiveDataContract
    {
        internal ByteArrayDataContract()
            : base(typeof(byte[]), DictionaryGlobals.ByteArrayLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class ObjectDataContract : PrimitiveDataContract
    {
        internal ObjectDataContract()
            : base(typeof(object), DictionaryGlobals.ObjectLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override bool CanContainReferences
        {
            get { return true; }
        }

        internal override bool IsPrimitive
        {
            get { return false; }
        }

    }

    internal class TimeSpanDataContract : PrimitiveDataContract
    {
        internal TimeSpanDataContract()
            : this(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SerializationNamespace)
        {
        }

        internal TimeSpanDataContract(XmlDictionaryString name, XmlDictionaryString ns)
            : base(typeof(TimeSpan), name, ns)
        {
        }
    }

    internal class XsDurationDataContract : TimeSpanDataContract
    {
        internal XsDurationDataContract() 
            : base(DictionaryGlobals.TimeSpanLocalName, DictionaryGlobals.SchemaNamespace) 
        {
        }
    }

    internal class GuidDataContract : PrimitiveDataContract
    {
        internal GuidDataContract()
            : this(DictionaryGlobals.GuidLocalName, DictionaryGlobals.SerializationNamespace)
        {
        }

        internal GuidDataContract(XmlDictionaryString name, XmlDictionaryString ns)
            : base(typeof(Guid), name, ns)
        {
        }
    }

    internal class AsmxGuidDataContract : GuidDataContract
    {
        internal AsmxGuidDataContract() 
            : base(DictionaryGlobals.GuidLocalName, DictionaryGlobals.AsmxTypesNamespace) 
        {
        }
    }

    internal class UriDataContract : PrimitiveDataContract
    {
        internal UriDataContract() 
            : base(typeof(Uri), DictionaryGlobals.UriLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }
    }

    internal class QNameDataContract : PrimitiveDataContract
    {
        internal QNameDataContract()
            : base(typeof(XmlQualifiedName), DictionaryGlobals.QNameLocalName, DictionaryGlobals.SchemaNamespace)
        {
        }

        internal override bool IsPrimitive
        {
            get { return false; }
        }
    }
}
