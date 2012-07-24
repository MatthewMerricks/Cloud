// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.ApplicationServer.Http.Moles;
    using Microsoft.ApplicationServer.Http.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass, UnitTestLevel(UnitTestLevel.InProgress)]
    public class MediaTypeFormatterCollectionTests : UnitTest<MediaTypeFormatterCollection>
    {
        #region Type

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection is public, concrete, and unsealed.")]
        public void TypeIsCorrect()
        {
            Type t = this.TypeUnderTest;
            Assert.IsTrue(t.IsPublic, "MediaTypeFormatterCollection should be public.");
            Assert.IsFalse(t.IsAbstract, "MediaTypeFormatterCollection should not be abstract.");
            Assert.IsFalse(t.IsSealed, "MediaTypeFormatterCollection should not be sealed.");
            Assert.AreEqual(typeof(Collection<MediaTypeFormatter>), t.BaseType, "BaseType is incorrect.");
        }

        #endregion Type

        #region Constructors

        #region MediaTypeFormatterCollection()

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection() sets XmlFormatter and JsonFormatter.")]
        public void Constructor()
        {
            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection();
            Assert.IsNotNull(collection.XmlFormatter, "XmlFormatter was not set.");
            Assert.IsNotNull(collection.JsonFormatter, "JsonFormatter was not set.");
        }

        #endregion MediaTypeFormatterCollection()

        #region MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>)

        [Ignore]
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>) sets XmlFormatter and JsonFormatter for all known collections of formatters.")]
        public void Constructor1()
        {
            // All combination of formatters presented to ctor should still set XmlFormatter
            foreach (IEnumerable<MediaTypeFormatter> formatterCollection in HttpTestData.AllFormatterCollections)
            {
                MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection(formatterCollection);
                Assert.IsNotNull(collection.XmlFormatter, "XmlFormatter was not set.");
                Assert.IsNotNull(collection.JsonFormatter, "JsonFormatter was not set.");
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>) sets derived classes of Xml and Json formatters.")]
        public void Constructor1SetsDerivedFormatters()
        {
            // force to array to get stable instances
            MediaTypeFormatter[] derivedFormatters = HttpTestData.DerivedFormatters.ToArray();
            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection(derivedFormatters);
            CollectionAssert.AreEqual(derivedFormatters, collection, "Derived formatters should have been in collection.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>) throws with null formatters collection.")]
        public void Constructor1ThrowsWithNullFormatters()
        {
            ExceptionAssert.ThrowsArgumentNull("formatters", () => new MediaTypeFormatterCollection(null));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>) throws with null formatter in formatters collection.")]
        public void Constructor1ThrowsWithNullFormatterInCollection()
        {
            ExceptionAssert.ThrowsArgument(
                "formatters",
                SR.CannotHaveNullInList(typeof(MediaTypeFormatter).Name),
                () => new MediaTypeFormatterCollection(new MediaTypeFormatter[] { null }));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>) accepts multiple instances of same formatter type.")]
        public void Constructor1AcceptsDuplicateFormatterTypes()
        {
            MediaTypeFormatter[] formatters = new MediaTypeFormatter[]
            {
                new XmlMediaTypeFormatter(),
                new JsonMediaTypeFormatter(),
                new XmlMediaTypeFormatter(),
                new JsonMediaTypeFormatter()
            };

            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection(formatters);
            CollectionAssert.AreEqual(formatters, collection, "Collections should have been identical");
        }

        #endregion MediaTypeFormatterCollection(IEnumerable<MediaTypeFormatter>)

        #endregion Constructors

        #region Properties
        #endregion Properties

        #region Methods
        #endregion Methods

        #region Base Methods

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection.Remove sets XmlFormatter to null.")]
        public void RemoveSetsXmlFormatter()
        {
            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection();
            int count = collection.Count;
            collection.Remove(collection.XmlFormatter);
            Assert.IsNull(collection.XmlFormatter, "Formatter was not cleared.");
            Assert.AreEqual(count - 1, collection.Count, "Collection count was incorrect.");
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection.Remove sets JsonFormatter to null.")]
        public void RemoveSetsJsonFormatter()
        {
            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection();
            int count = collection.Count;
            collection.Remove(collection.JsonFormatter);
            Assert.IsNull(collection.JsonFormatter, "Formatter was not cleared.");
            Assert.AreEqual(count - 1, collection.Count, "Collection count was incorrect.");
        }

        [TestMethod, HostType("Moles")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        [Description("MediaTypeFormatterCollection.Insert sets XmlFormatter.")]
        public void InsertSetsXmlFormatter()
        {
            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection();
            int count = collection.Count;
            SXmlMediaTypeFormatter formatter = new SXmlMediaTypeFormatter();
            collection.Insert(0, formatter);
            Assert.AreSame(formatter, collection.XmlFormatter, "Formatter was set.");
            Assert.AreEqual(count + 1, collection.Count, "Collection count was incorrect.");
        }

        [TestMethod, HostType("Moles")]
        [Description("MediaTypeFormatterCollection.Insert sets JsonFormatter.")]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("")]
        public void InsertSetsJsonFormatter()
        {
            MediaTypeFormatterCollection collection = new MediaTypeFormatterCollection();
            int count = collection.Count;
            SJsonMediaTypeFormatter formatter = new SJsonMediaTypeFormatter();
            collection.Insert(0, formatter);
            Assert.AreSame(formatter, collection.JsonFormatter, "Formatter was set.");
            Assert.AreEqual(count + 1, collection.Count, "Collection count was incorrect.");
        }

        #endregion Base Methods
    }
}
