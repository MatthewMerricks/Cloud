// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.ApplicationServer.Common.Test.Types;

    /// <summary>
    /// A base class for test data.  A <see cref="TestData"/> instance is associated with a given type, and the <see cref="TestData"/> instance can
    /// provide instances of the given type to use as data in tests.  The same <see cref="TestData"/> instance can also provide instances
    /// of types related to the given type, such as a <see cref="List<>"/> of the type.  See the <see cref="TestDataFlags"/> enum for all the
    /// variations of test data that a <see cref="TestData"/> instance can provide.
    /// </summary>
    public abstract class TestData
    {
        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="char"/>.
        /// </summary>
        public static readonly ValueTypeTestData<char> CharTestData = new ValueTypeTestData<char>('a', char.MinValue, char.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="int"/>.
        /// </summary>
        public static readonly ValueTypeTestData<int> IntTestData = new ValueTypeTestData<int>(-1, 0, 1, int.MinValue, int.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="uint"/>.
        /// </summary>
        public static readonly ValueTypeTestData<uint> UintTestData = new ValueTypeTestData<uint>(0, 1, uint.MinValue, uint.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="short"/>.
        /// </summary>
        public static readonly ValueTypeTestData<short> ShortTestData = new ValueTypeTestData<short>(-1, 0, 1, short.MinValue, short.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="ushort"/>.
        /// </summary>
        public static readonly ValueTypeTestData<ushort> UshortTestData = new ValueTypeTestData<ushort>(0, 1, ushort.MinValue, ushort.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="long"/>.
        /// </summary>
        public static readonly ValueTypeTestData<long> LongTestData = new ValueTypeTestData<long>(-1, 0, 1, long.MinValue, long.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="ulong"/>.
        /// </summary>
        public static readonly ValueTypeTestData<ulong> UlongTestData = new ValueTypeTestData<ulong>(0, 1, ulong.MinValue, ulong.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="byte"/>.
        /// </summary>
        public static readonly ValueTypeTestData<byte> ByteTestData = new ValueTypeTestData<byte>(0, 1, byte.MinValue, byte.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="sbyte"/>.
        /// </summary>
        public static readonly ValueTypeTestData<sbyte> SByteTestData = new ValueTypeTestData<sbyte>(-1, 0, 1, sbyte.MinValue, sbyte.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="bool"/>.
        /// </summary>
        public static readonly ValueTypeTestData<bool> BoolTestData = new ValueTypeTestData<bool>(true, false);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="double"/>.
        /// </summary>
        public static readonly ValueTypeTestData<double> DoubleTestData = new ValueTypeTestData<double>(
            -1.0, 
            0.0, 
            1.0, 
            double.MinValue, 
            double.MaxValue, 
            double.PositiveInfinity, 
            double.NegativeInfinity);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="float"/>.
        /// </summary>
        public static readonly ValueTypeTestData<float> FloatTestData = new ValueTypeTestData<float>(
            -1.0f, 
            0.0f, 
            1.0f, 
            float.MinValue, 
            float.MaxValue, 
            float.PositiveInfinity, 
            float.NegativeInfinity);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="decimal"/>.
        /// </summary>
        public static readonly ValueTypeTestData<decimal> DecimalTestData = new ValueTypeTestData<decimal>(
            -1M, 
            0M, 
            1M, 
            decimal.MinValue, 
            decimal.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="DateTime"/>.
        /// </summary>
        public static readonly ValueTypeTestData<DateTime> DateTimeTestData = new ValueTypeTestData<DateTime>(
            DateTime.Now, 
            DateTime.UtcNow, 
            DateTime.MaxValue, 
            DateTime.MinValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="TimeSpan"/>.
        /// </summary>
        public static readonly ValueTypeTestData<TimeSpan> TimeSpanTestData = new ValueTypeTestData<TimeSpan>(
            TimeSpan.MinValue, 
            TimeSpan.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="Guid"/>.
        /// </summary>
        public static readonly ValueTypeTestData<Guid> GuidTestData = new ValueTypeTestData<Guid>(
            Guid.NewGuid(), 
            Guid.Empty);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="DateTimeOffset"/>.
        /// </summary>
        public static readonly ValueTypeTestData<DateTimeOffset> DateTimeOffsetTestData = new ValueTypeTestData<DateTimeOffset>(
            DateTimeOffset.MaxValue, 
            DateTimeOffset.MinValue, 
            new DateTimeOffset(DateTime.Now));

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c>.
        /// </summary>
        public static readonly ValueTypeTestData<SimpleEnum> SimpleEnumTestData = new ValueTypeTestData<SimpleEnum>(
            SimpleEnum.First, 
            SimpleEnum.Second, 
            SimpleEnum.Third);

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c> implemented with a <see cref="long"/>.
        /// </summary>
        public static readonly ValueTypeTestData<LongEnum> LongEnumTestData = new ValueTypeTestData<LongEnum>(
            LongEnum.FirstLong, 
            LongEnum.SecondLong, 
            LongEnum.ThirdLong);

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c> decorated with a <see cref="FlagsAttribtute"/>.
        /// </summary>
        public static readonly ValueTypeTestData<FlagsEnum> FlagsEnumTestData = new ValueTypeTestData<FlagsEnum>(
            FlagsEnum.One, 
            FlagsEnum.Two, 
            FlagsEnum.Four);

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c> decorated with a <see cref="DataContractAttribute"/>.
        /// </summary>
        public static readonly ValueTypeTestData<DataContractEnum> DataContractEnumTestData = new ValueTypeTestData<DataContractEnum>(
            DataContractEnum.First, 
            DataContractEnum.Second);

        /// <summary>
        /// All expected permutations of an empty string.
        /// </summary>
        public static readonly TestData<string> EmptyStrings = new RefTypeTestData<string>(() => new List<string>() { null, string.Empty, "", " ", "\t\r\n" });

        /// <summary>
        ///  Common <see cref="TestData"/> for the string form of a <see cref="Uri"/>.
        /// </summary>
        public static readonly RefTypeTestData<string> UriTestDataStrings = new RefTypeTestData<string>(() => new List<string>(){ 
            "http://somehost", 
            "http://somehost:8080", 
            "http://somehost/",
            "http://somehost:8080/", 
            "http://somehost/somepath", 
            "http://somehost/somepath/",
            "http://somehost/somepath?somequery=somevalue"});

        /// <summary>
        ///  Common <see cref="TestData"/> for a <see cref="Uri"/>.
        /// </summary>
        public static readonly RefTypeTestData<Uri> UriTestData = new RefTypeTestData<Uri>(() => 
            UriTestDataStrings.Select<string, Uri>((s) => new Uri(s)).ToList());


        /// <summary>
        ///  Common <see cref="TestData"/> for a <see cref="string"/>.
        /// </summary>
        public static readonly RefTypeTestData<string> StringTestData = new RefTypeTestData<string>(() => new List<string>() {
            "",
            " ",            // one space
            "  ",           // multiple spaces
            " data ",       // leading and trailing whitespace
            "\t\t \n ", 
            "Some String!"});

        /// <summary>
        ///  Common <see cref="TestData"/> for a POCO class type.
        /// </summary>
        public static readonly RefTypeTestData<PocoType> PocoTypeTestData = new RefTypeTestData<PocoType>(
            PocoType.GetTestData, 
            PocoType.GetDerivedTypeTestData, 
            null);

        /// <summary>
        ///  Common <see cref="TestData"/> for a POCO class type that includes null values
        ///  for both the base class and derived classes.
        /// </summary>
        public static readonly RefTypeTestData<PocoType> PocoTypeTestDataWithNull = new RefTypeTestData<PocoType>(
            PocoType.GetTestDataWithNull,
            PocoType.GetDerivedTypeTestDataWithNull,
            null);

        /// <summary>
        ///  Common <see cref="TestData"/> for a class type decorated with DataContract attributes.
        /// </summary>
        public static readonly RefTypeTestData<DataContractType> DataContractTypeTestData = new RefTypeTestData<DataContractType>(
            DataContractType.GetTestData, 
            DataContractType.GetDerivedTypeTestData, 
            null);

        /// <summary>
        ///  Common <see cref="TestData"/> for a class type decorated with DataContract attributes that derives from a base DataContract class type.
        /// </summary>
        public static readonly RefTypeTestData<DerivedDataContractType> DerivedDataContractTypeTestData = new RefTypeTestData<DerivedDataContractType>(
            DerivedDataContractType.GetTestData, 
            null, 
            DerivedDataContractType.GetKnownTypeTestData);

        /// <summary>
        ///  Common <see cref="TestData"/> for a class type decorated with DataContract attributes and that has 
        ///  <see cref="DataContactAttribute.IsReference"/> set to <c>true</c>.
        /// </summary>
        public static readonly RefTypeTestData<ReferenceDataContractType> ReferenceDataContractTypeTestData = new RefTypeTestData<ReferenceDataContractType>(
            ReferenceDataContractType.GetTestData);

        /// <summary>
        ///  Common <see cref="TestData"/> for a class type decorated with XmlSerializer attributes.
        /// </summary>
        public static readonly RefTypeTestData<XmlSerializableType> XmlSerializableTypeTestData = new RefTypeTestData<XmlSerializableType>(
            XmlSerializableType.GetTestData, 
            XmlSerializableType.GetDerivedTypeTestData, 
            null);

        /// <summary>
        ///  Common <see cref="TestData"/> for a class type decorated with XmlSerializer attributes that derives from a base XmlSerializerType class.
        /// </summary>
        public static readonly RefTypeTestData<DerivedXmlSerializableType> DerivedXmlSerializableTypeTestData = new RefTypeTestData<DerivedXmlSerializableType>(
            DerivedXmlSerializableType.GetTestData, 
            null, 
            DerivedXmlSerializableType.GetKnownTypeTestData);

        /// <summary>
        ///  Common <see cref="TestData"/> for a class that implements <see cref="ISerializable"/>.
        /// </summary>
        public static readonly RefTypeTestData<ISerializableType> ISerializableTypeTestData = new RefTypeTestData<ISerializableType>(
            ISerializableType.GetTestData);
        
        /// <summary>
        /// A read-only collection of value type test data.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> ValueTypeTestDataCollection = new ReadOnlyCollection<TestData>(new TestData[] {
            CharTestData, 
            IntTestData, 
            UintTestData, 
            ShortTestData, 
            UshortTestData, 
            LongTestData, 
            UlongTestData, 
            ByteTestData, 
            SByteTestData, 
            BoolTestData,
            DoubleTestData, 
            FloatTestData, 
            DecimalTestData, 
            TimeSpanTestData, 
            GuidTestData, 
            DateTimeOffsetTestData, 
            SimpleEnumTestData, 
            LongEnumTestData,
            FlagsEnumTestData, 
            DataContractEnumTestData});

        /// <summary>
        /// A read-only collection of reference type test data.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> RefTypeTestDataCollection = new ReadOnlyCollection<TestData>(new TestData[] { 
            StringTestData, 
            PocoTypeTestData, 
            DataContractTypeTestData, 
            DerivedDataContractTypeTestData, 
            XmlSerializableTypeTestData, 
            DerivedXmlSerializableTypeTestData, 
            ISerializableTypeTestData,  
            ReferenceDataContractTypeTestData});

        /// <summary>
        /// A read-only collection of value and reference type test data.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> ValueAndRefTypeTestDataCollection = new ReadOnlyCollection<TestData>(
            ValueTypeTestDataCollection.Concat(RefTypeTestDataCollection).ToList());

        /// <summary>
        /// A read-only collection of representative values and reference type test data.
        /// Uses where exhaustive coverage is not required.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> RepresentativeValueAndRefTypeTestDataCollection = new ReadOnlyCollection<TestData>(new TestData[] {
            IntTestData,
            BoolTestData,
            SimpleEnumTestData,
            StringTestData, 
            PocoTypeTestData
        });

        private Dictionary<TestDataVariations, TestDataVariationProvider> registeredTestDataVariations;


        /// <summary>
        /// Initializes a new instance of the <see cref="TestData"/> class.
        /// </summary>
        /// <param name="type">The type associated with the <see cref="TestData"/> instance.</param>
        protected TestData(Type type)
        {
            if (type.ContainsGenericParameters)
            {
                throw new InvalidOperationException("Only closed generic types are supported.");
            }

            this.Type = type;
            this.registeredTestDataVariations = new Dictionary<TestDataVariations, TestDataVariationProvider>();
        }

        /// <summary>
        /// Gets the type associated with the <see cref="TestData"/> instance.
        /// </summary>
        public Type Type { get; private set; }


        /// <summary>
        /// Gets the supported test data variations.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TestDataVariations>  GetSupportedTestDataVariations()
        {
            return this.registeredTestDataVariations.Keys;
        }

        /// <summary>
        /// Gets the related type for the given test data variation or returns null if the <see cref="TestData"/> instance
        /// doesn't support the given variation.
        /// </summary>
        /// <param name="variation">The test data variation with which to create the related <see cref="Type"/>.</param>
        /// <returns>The related <see cref="Type"/> for the <see cref="TestData.Type"/> as given by the test data variation.</returns>
        /// <example>
        /// For example, if the given <see cref="TestData"/> was created for <see cref="string"/> test data and the varation parameter
        /// was <see cref="TestDataFlags.AsList"/> then the returned type would be <see cref="List<string>"/>.
        /// </example>
        public Type GetAsTypeOrNull(TestDataVariations variation)
        {
            TestDataVariationProvider testDataVariation = null;
            if (this.registeredTestDataVariations.TryGetValue(variation, out testDataVariation))
            {
                return testDataVariation.Type;
            }

            return null;
        }

        /// <summary>
        /// Gets test data for the given test data variation or returns null if the <see cref="TestData"/> instance
        /// doesn't support the given variation.
        /// </summary>
        /// <param name="variation">The test data variation with which to create the related test data.</param>
        /// <returns>Test data of the type specified by the <see cref="TestData.GetAsTypeOrNull"/> method.</returns>
        public object GetAsTestDataOrNull(TestDataVariations variation)
        {
            TestDataVariationProvider testDataVariation = null;
            if (this.registeredTestDataVariations.TryGetValue(variation, out testDataVariation))
            {
                return testDataVariation.TestDataProvider();
            }

            return null;
        }


        /// <summary>
        /// Allows derived classes to register a <paramref name="testDataProvider "/> <see cref="Func<>"/> that will 
        /// provide test data for a given variation.
        /// </summary>
        /// <param name="variation">The variation with which to register the <paramref name="testDataProvider "/>r.</param>
        /// <param name="type">The type of the test data created by the <paramref name="testDataProvider "/></param>
        /// <param name="testDataProvider">A <see cref="Func<>"/> that will provide test data.</param>
        protected void RegisterTestDataVariation(TestDataVariations variation, Type type, Func<object> testDataProvider)
        {
            this.registeredTestDataVariations.Add(variation, new TestDataVariationProvider(type, testDataProvider));
        }

        private class TestDataVariationProvider
        {
            public TestDataVariationProvider(Type type, Func<object> testDataProvider)
            {
                this.Type = type;
                this.TestDataProvider = testDataProvider;
            }


            public Func<object> TestDataProvider { get; private set; }

            public Type Type { get; private set; }
        }
    }


    /// <summary>
    /// A generic base class for test data. 
    /// </summary>
    /// <typeparam name="T">The type associated with the test data.</typeparam>
    public abstract class TestData<T> : TestData, IEnumerable<T>
    {
        private static readonly Type OpenIEnumerableType = typeof(IEnumerable<>);
        private static readonly Type OpenListType = typeof(List<>);
        private static readonly Type OpenIQueryableType = typeof(IQueryable<>);
        private static readonly Type OpenGenericDataContractType = typeof(GenericDataContractType<>);
        private static readonly Type OpenGenericXmlSerializableType = typeof(GenericXmlSerializableType<>);

        /// <summary>
        /// Initializes a new instance of the <see cref="TestData&lt;T&gt;"/> class.
        /// </summary>
        protected TestData() 
            : base(typeof(T))
        {
            Type[] typeParams = new Type[] { this.Type };
            
            Type arrayType = this.Type.MakeArrayType();
            Type listType = OpenListType.MakeGenericType(typeParams);
            Type iEnumerableType = OpenIEnumerableType.MakeGenericType(typeParams);
            Type iQueryableType = OpenIQueryableType.MakeGenericType(typeParams);
     
            Type[] typeArrayParams = new Type[] { arrayType };
            Type[] typeListParams = new Type[] { listType };
            Type[] typeIEnumerableParams = new Type[] { iEnumerableType };
            Type[] typeIQueryableParams = new Type[] { iQueryableType };

            this.RegisterTestDataVariation(TestDataVariations.AsInstance, this.Type, () => GetTypedTestData());
            this.RegisterTestDataVariation(TestDataVariations.AsArray, arrayType, GetTestDataAsArray);
            this.RegisterTestDataVariation(TestDataVariations.AsIEnumerable, iEnumerableType, GetTestDataAsIEnumerable);
            this.RegisterTestDataVariation(TestDataVariations.AsIQueryable, iQueryableType, GetTestDataAsIQueryable);
            this.RegisterTestDataVariation(TestDataVariations.AsList, listType, GetTestDataAsList);
            
            Type dataContractPropertyType = OpenGenericDataContractType.MakeGenericType(typeParams);
            Type dataContractArrayPropertyType = OpenGenericDataContractType.MakeGenericType(typeArrayParams);
            Type dataContractListPropertyType = OpenGenericDataContractType.MakeGenericType(typeListParams);
            Type dataContractIEnumerablePropertyType = OpenGenericDataContractType.MakeGenericType(typeIEnumerableParams);
            Type dataContractIQueryablePropertyType = OpenGenericDataContractType.MakeGenericType(typeIQueryableParams);

            this.RegisterTestDataVariation(TestDataVariations.AsDataMember, dataContractPropertyType, GetAsInstancePropertyOfDataContractType);
            this.RegisterTestDataVariation(TestDataVariations.AsDataMember | TestDataVariations.AsArray, dataContractArrayPropertyType, GetAsArrayPropertyOfDataContractType);
            this.RegisterTestDataVariation(TestDataVariations.AsDataMember | TestDataVariations.AsList, dataContractListPropertyType, GetAsListPropertyOfDataContractType);
            this.RegisterTestDataVariation(TestDataVariations.AsDataMember | TestDataVariations.AsIEnumerable, dataContractIEnumerablePropertyType, GetAsIEnumerablePropertyOfDataContractType);
            this.RegisterTestDataVariation(TestDataVariations.AsDataMember | TestDataVariations.AsIQueryable, dataContractIQueryablePropertyType, GetAsIQueryablePropertyOfDataContractType);

            Type xmlSerializablePropertyType = OpenGenericXmlSerializableType.MakeGenericType(typeParams);
            Type xmlSerializableArrayPropertyType = OpenGenericXmlSerializableType.MakeGenericType(typeArrayParams);
            Type xmlSerializableListPropertyType = OpenGenericXmlSerializableType.MakeGenericType(typeListParams);
            Type xmlSerializableIEnumerablePropertyType = OpenGenericXmlSerializableType.MakeGenericType(typeIEnumerableParams);
            Type xmlSerializableIQueryablePropertyType = OpenGenericXmlSerializableType.MakeGenericType(typeIQueryableParams);

            this.RegisterTestDataVariation(TestDataVariations.AsXmlElementProperty, xmlSerializablePropertyType, GetAsInstancePropertyOfXmlSerializableType);
            this.RegisterTestDataVariation(TestDataVariations.AsXmlElementProperty | TestDataVariations.AsArray, xmlSerializableArrayPropertyType, GetAsArrayPropertyOfXmlSerializableType);
            this.RegisterTestDataVariation(TestDataVariations.AsXmlElementProperty | TestDataVariations.AsList, xmlSerializableListPropertyType, GetAsListPropertyOfXmlSerializableType);
            this.RegisterTestDataVariation(TestDataVariations.AsXmlElementProperty | TestDataVariations.AsIEnumerable, xmlSerializableIEnumerablePropertyType, GetAsIEnumerablePropertyOfXmlSerializableType);
            this.RegisterTestDataVariation(TestDataVariations.AsXmlElementProperty | TestDataVariations.AsIQueryable, xmlSerializableIQueryablePropertyType, GetAsIQueryablePropertyOfXmlSerializableType);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)this.GetTypedTestData().ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetTypedTestData().ToList().GetEnumerator();
        }

        /// <summary>
        /// Gets the test data as an array.
        /// </summary>
        /// <returns>An array of test data of the given type.</returns>
        public T[] GetTestDataAsArray()
        {
            return this.GetTypedTestData().ToArray();
        }

        /// <summary>
        /// Gets the test data as a <see cref="List<>"/>.
        /// </summary>
        /// <returns>A <see cref="List<>"/> of test data of the given type.</returns>
        public List<T> GetTestDataAsList()
        {
            return this.GetTypedTestData().ToList();
        }

        /// <summary>
        /// Gets the test data as an <see cref="IEnumerable<>"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerable<>"/> of test data of the given type.</returns>
        public IEnumerable<T> GetTestDataAsIEnumerable()
        {
            return this.GetTypedTestData().AsEnumerable();
        }

        /// <summary>
        /// Gets the test data as an <see cref="IQueryable<>"/>.
        /// </summary>
        /// <returns>An <see cref="IQueryable<>"/> of test data of the given type.</returns>
        public IQueryable<T> GetTestDataAsIQueryable()
        {
            return this.GetTypedTestData().AsQueryable();
        }

        /// <summary>
        /// Gets a collection of DataContract type instances with a DataMember of the given type.
        /// </summary>
        /// <returns>A collection of DataContract type instances with a DataMember of the given type.</returns>
        public IEnumerable<GenericDataContractType<T>> GetAsInstancePropertyOfDataContractType()
        {
            return this.GetTypedTestData().Select(t => new GenericDataContractType<T>(t));
        }

        /// <summary>
        /// Gets a DataContract instance with a property with an array of the given type.
        /// </summary>
        /// <returns>A DataContract instance with a property with an array of the given type.</returns>
        public GenericDataContractType<T[]> GetAsArrayPropertyOfDataContractType()
        {
            return new GenericDataContractType<T[]>(this.GetTestDataAsArray());
        }

        /// <summary>
        /// Gets a DataContract instance with a property with a <see cref="List<>"/> of the given type.
        /// </summary>
        /// <returns>A DataContract instance with a property with a <see cref="List<>"/> of the given type.</returns>
        public GenericDataContractType<List<T>> GetAsListPropertyOfDataContractType()
        {
            return new GenericDataContractType<List<T>>(this.GetTestDataAsList());
        }

        /// <summary>
        /// Gets a DataContract instance with a property with an <see cref="IEnumerable<>"/> of the given type.
        /// </summary>
        /// <returns>A DataContract instance with a property with an <see cref="IEnumerable<>"/> of the given type.</returns>
        public GenericDataContractType<IEnumerable<T>> GetAsIEnumerablePropertyOfDataContractType()
        {
            return new GenericDataContractType<IEnumerable<T>>(this.GetTestDataAsIEnumerable());
        }

        /// <summary>
        /// Gets a DataContract instance with a property with an <see cref="IQueryable<>"/> of the given type.
        /// </summary>
        /// <returns>A DataContract instance with a property with an <see cref="IQueryable<>"/> of the given type.</returns>
        public GenericDataContractType<IQueryable<T>> GetAsIQueryablePropertyOfDataContractType()
        {
            return new GenericDataContractType<IQueryable<T>>(this.GetTestDataAsIQueryable());
        }

        /// <summary>
        /// Gets a collection of XmlSerializable type instances with an <see cref="XmlElementAttribute"/> property of the given type.
        /// </summary>
        /// <returns>A collection of XmlSerializable type instances with an <see cref="XmlElementAttribute"/> property of the given type.</returns>
        public IEnumerable<GenericXmlSerializableType<T>> GetAsInstancePropertyOfXmlSerializableType()
        {
            return this.GetTypedTestData().Select(t => new GenericXmlSerializableType<T>(t));
        }

        /// <summary>
        /// Gets an XmlSerializable instance with a property with an array of the given type.
        /// </summary>
        /// <returns>An XmlSerializable instance with a property with an array of the given type.</returns>
        public GenericXmlSerializableType<T[]> GetAsArrayPropertyOfXmlSerializableType()
        {
            return new GenericXmlSerializableType<T[]>(this.GetTestDataAsArray());
        }

        /// <summary>
        /// Gets an XmlSerializable instance with a property with an <see cref="List<>"/> of the given type.
        /// </summary>
        /// <returns>An XmlSerializable instance with a property with an <see cref="List<>"/> of the given type.</returns>
        public GenericXmlSerializableType<List<T>> GetAsListPropertyOfXmlSerializableType()
        {
            return new GenericXmlSerializableType<List<T>>(this.GetTestDataAsList());
        }

        /// <summary>
        /// Gets an XmlSerializable instance with a property with an <see cref="IEnumerable<>"/> of the given type.
        /// </summary>
        /// <returns>An XmlSerializable instance with a property with an <see cref="IEnumerable<>"/> of the given type.</returns>
        public GenericXmlSerializableType<IEnumerable<T>> GetAsIEnumerablePropertyOfXmlSerializableType()
        {
            return new GenericXmlSerializableType<IEnumerable<T>>(this.GetTestDataAsIEnumerable());
        }

        /// <summary>
        /// Gets an XmlSerializable instance with a property with an <see cref="IQueryable<>"/> of the given type.
        /// </summary>
        /// <returns>An XmlSerializable instance with a property with an <see cref="IQueryable<>"/> of the given type.</returns>
        public GenericXmlSerializableType<IQueryable<T>> GetAsIQueryablePropertyOfXmlSerializableType()
        {
            return new GenericXmlSerializableType<IQueryable<T>>(this.GetTestDataAsIQueryable());
        }

        /// <summary>
        /// Must be implemented by derived types to return test data of the given type.
        /// </summary>
        /// <returns>Test data of the given type.</returns>
        protected abstract IEnumerable<T> GetTypedTestData();
    }
}
