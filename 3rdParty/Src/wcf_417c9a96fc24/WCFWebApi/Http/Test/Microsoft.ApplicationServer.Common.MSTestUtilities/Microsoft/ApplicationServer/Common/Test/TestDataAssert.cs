// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.ApplicationServer.Common.Test
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationServer.Common.Test.Types;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections;

    /// <summary>
    /// MSTest utility for testing code against common test data.
    /// </summary>
    public static class TestDataAssert
    {
        private static readonly Type nameAndIdContainerType = typeof(INameAndIdContainer);
        private static readonly Type genericValueContainerType = typeof(IGenericValueContainer);
        private static readonly Type referenceDataContractType = typeof(ReferenceDataContractType);

        public static void Execute(IEnumerable<TestData> testData, Action<Type, object> codeUnderTest)
        {
            Execute(testData, TestDataVariations.All, null, codeUnderTest);
        }

        public static void Execute(IEnumerable<TestData> testDataCollection, TestDataVariations flags, string messageOnFail, Action<Type, object> codeUnderTest)
        {
            if (testDataCollection == null)
            {
                throw new ArgumentNullException("testData");
            }

            if (codeUnderTest == null)
            {
                throw new ArgumentNullException("codeUnderTest");
            }

            foreach (TestData testdataInstance in testDataCollection)
            {
                foreach (TestDataVariations variation in testdataInstance.GetSupportedTestDataVariations())
                {
                    if ((variation & flags) == variation)
                    {
                        Type variationType = testdataInstance.GetAsTypeOrNull(variation);
                        object testData = testdataInstance.GetAsTestDataOrNull(variation);
                        if (AsSingleInstances(variation))
                        {
                            foreach (object obj in (IEnumerable)testData)
                            {
                                ExecuteCodeUnderTest(variationType, obj, messageOnFail, codeUnderTest);
                            }
                        }
                        else
                        {
                            ExecuteCodeUnderTest(variationType, testData, messageOnFail, codeUnderTest);
                        }
                    }
                }
            }
        }

        private static bool AsSingleInstances(TestDataVariations variation)
        {
            return variation == TestDataVariations.AsInstance ||
                   variation == TestDataVariations.AsNullable ||
                   variation == TestDataVariations.AsDerivedType ||
                   variation == TestDataVariations.AsKnownType ||
                   variation == TestDataVariations.AsDataMember ||
                   variation == TestDataVariations.AsXmlElementProperty;
        }

        private static void ExecuteCodeUnderTest(Type type, object obj, string messageOnFail, Action<Type,object> codeUnderTest)
        {
            Exception actualException = null;
 	        AssertFailedException assertFailedException = null;

            try
            {
                codeUnderTest(type, obj);
            }
            catch (Exception exception)
            {
                actualException = exception;

                // Let assert failure in the callback escape these checks
                assertFailedException = exception as AssertFailedException;
                if (assertFailedException != null)
                {
                    throw;
                }
            }
            finally
            {
                if (assertFailedException == null && actualException != null)
                {
                    string failureMessage = null;
                    if (type.IsValueType)
                    {
                        failureMessage = string.Format("Executing with an instance of type '{0}' and value '{1}' failed.", type.FullName, obj.ToString());
                    }
                    else
                    {
                        failureMessage = string.Format("Executing with an instance of type '{0}' failed.", type.FullName);
                    }

                    failureMessage = string.Format("{0}: {1}.", failureMessage, actualException.Message);

                    if (!string.IsNullOrWhiteSpace(messageOnFail))
                    {
                        failureMessage = string.Format(" {0}", failureMessage);
                    }

                    throw new AssertFailedException(failureMessage, actualException);
                }
            }
        }

        /// <summary>
        /// Smarter version of MSTest equality check that redirects to <see cref="CollectionAssert"/>
        /// when appropriate.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="failureMessage">Message to use as prefix for any assertion failures.</param>
        public static void AreEqual(object expected, object actual, string failureMessage)
        {
            if (expected != null && expected is ICollection)
            {
                ICollection expectedCollection = expected as ICollection;
                ICollection actualCollection = actual as ICollection;
                Assert.IsNotNull(actualCollection, string.Format("{0}  Expected was type '{1}' but actual was null.", failureMessage, expected.GetType()));
                Assert.AreEqual(expectedCollection.Count, actualCollection.Count, string.Format("{0}  Collections of type '{1}' differ in size.", failureMessage, expected.GetType()));
                object[] expectedArray = new object[expectedCollection.Count];
                expectedCollection.CopyTo(expectedArray, 0);
                object[] actualArray = new object[actualCollection.Count];
                actualCollection.CopyTo(actualArray, 0);
                for (int i = 0; i < expectedArray.Length; ++i)
                {
                    AreEqual(expectedArray[i], actualArray[i], string.Format("{0}  Element[{1}] of '{2}' differ in size.", failureMessage, i, expected.GetType()));
                }
            }
            else
            {
                if (expected == null)
                {
                    string actualAsString = actual == null ? "<null>" : actual.ToString();
                    Assert.IsNull(actual, string.Format("{0}: expected null but found '{1}'.", failureMessage, actualAsString));
                }
                else
                {
                    // expected != null
                    Assert.IsNotNull(actual, string.Format("{0}: expected '{1}' but found null.", failureMessage, expected.ToString()));

                    if (nameAndIdContainerType.IsAssignableFrom(expected.GetType()))
                    {
                        AreEqual(expected as INameAndIdContainer, actual as INameAndIdContainer, failureMessage);
                    }
                    else if (genericValueContainerType.IsAssignableFrom(expected.GetType()))
                    {
                        AreEqual(expected as IGenericValueContainer, actual as IGenericValueContainer, failureMessage);
                    }
                    else if (referenceDataContractType.IsAssignableFrom(expected.GetType()))
                    {
                        AreEqual(expected as ReferenceDataContractType, actual as ReferenceDataContractType, failureMessage);
                    }
                    else
                    {
                        if (!expected.Equals(actual))
                        {
                            // It is acceptable for whitespace in strings to be trimmed when round-tripped
                            string expectedAsString = expected as string;
                            string actualAsString = actual as string;
                            if (expectedAsString != null && actualAsString != null)
                            {
                                expectedAsString = expectedAsString.Trim();
                                actualAsString = actualAsString.Trim();
                                AreEqual(expectedAsString, actualAsString, string.Format("{0}  Trimmed strings should have been the same.", failureMessage));
                            }
                            else
                            {
                                Assert.AreEqual(expected, actual, failureMessage);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asserts the two given <see cref="INameAndContainer"/> instances are equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="errorMessage">Message to prefix to assertion failures.</param>
        public static void AreEqual(INameAndIdContainer expected, INameAndIdContainer actual, string errorMessage)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, string.Format("{0}  actual value was not null but expected value was.", errorMessage));
            }

            Assert.IsNotNull(actual, string.Format("{0}  actual value was null but expected value was not.", errorMessage));

            Assert.AreEqual(expected.GetType(), actual.GetType(), string.Format("{0}  actual value was not of the same type as the expected.", errorMessage));

            Assert.AreEqual(expected.Name, actual.Name, string.Format("{0}  Name property was incorrect.", errorMessage));

            Assert.AreEqual(expected.Id, actual.Id, string.Format("{0}  Id property was incorrect.", errorMessage));
        }

        /// <summary>
        /// Asserts the two given <see cref="IGenericValueContainer"/> instances are equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="errorMessage">Message to prefix to assertion failures.</param>
        public static void AreEqual(IGenericValueContainer expected, IGenericValueContainer actual, string errorMessage)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, string.Format("{0}  actual value was not null but expected value was.", errorMessage));
            }

            Assert.IsNotNull(actual, string.Format("{0}  actual value was null but expected value was not.", errorMessage));

            Assert.AreEqual(expected.GetType(), actual.GetType(), string.Format("{0}  actual value was not of the same type as the expected.", errorMessage));

            object expectedValue = expected.GetValue();
            object actualValue = actual.GetValue();

            AreEqual(expectedValue, actualValue, errorMessage);
        }

        /// <summary>
        /// Asserts the two given <see cref="ReferenceDataContractType"/> instances are equal.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="failureMessage">Message to prefix to assertion failures.</param>
        public static void AreEqual(ReferenceDataContractType expected, ReferenceDataContractType actual, string failureMessage)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, string.Format("{0}  actual value was not null but expected value was.", failureMessage));
            }

            Assert.IsNotNull(actual, string.Format("{0}  actual value was null but expected value was not.", failureMessage));

            Assert.AreEqual(expected.GetType(), actual.GetType(), string.Format("{0}  actual value was not of the same type as the expected.", failureMessage));

            ReferenceDataContractType expectedOther = expected.Other;
            ReferenceDataContractType actualOther = actual.Other;

            if (expectedOther == null)
            {
                Assert.IsNull(actualOther, string.Format("{0}  actual.Other was not null but expected.Other was.", failureMessage));
            }
            else
            {
                Assert.IsNotNull(actualOther, string.Format("{0}  actual.Other was null but expected.Other was not.", failureMessage));
                Assert.AreEqual(expectedOther.GetType(), actualOther.GetType(), string.Format("{0}  the 'Other' properties contained objects of different types.", failureMessage));
            }
        }
    }
}
