// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http.Description
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Common.Test;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test.Services;

    [TestClass]
    public class HttpParameterCollectionTests
    {
        #region Constructor Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection supports default ctor")]
        public void HttpParameterCollection_Default_Ctor()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            Assert.AreEqual(0, coll.Count, "Expected default ctor to init to empty collection");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection can be initialized from empty list")]
        public void HttpParameterCollection_Ctor_Empty_List()
        {
            HttpParameterCollection coll = new HttpParameterCollection(Enumerable.Empty<HttpParameter>().ToList());
            Assert.AreEqual(0, coll.Count, "Expected empty list to init to empty collection");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection throws for null ctor descriptions parameter")]
        public void HttpParameterCollection_Ctor_Throws_Null_Descriptions()
        {
            ExceptionAssert.ThrowsArgumentNull("parameters", () => new HttpParameterCollection(parameters: null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection can be constructed from mock HttpParameters")]
        public void HttpParameterCollection_Ctor_From_Mock_HttpParameters()
        {
            HttpParameter[] paramDescs = new[] {
                new HttpParameter("First", typeof(string)),
                new HttpParameter("Second", typeof(int)),
            };

            HttpParameterCollection coll = new HttpParameterCollection(paramDescs.ToList());
            Assert.AreEqual(2, coll.Count, "HttpParameterCollection should have found 2 parameters");

            Assert.AreEqual("First", coll[0].Name, "Name1 was not set correctly");
            Assert.AreEqual(typeof(string), coll[0].Type, "Type1 was not set correctly");

            Assert.AreEqual("Second", coll[1].Name, "Name2 was not set correctly");
            Assert.AreEqual(typeof(int), coll[1].Type, "Type2 was not set correctly");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection can be constructed from input MessagePartDescriptionCollection")]
        public void HttpParameterCollection_Ctor_From_Input_MessagePartDescriptionCollection()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            HttpParameterCollection coll = new HttpParameterCollection(od, isOutputCollection: false);
            Assert.IsNotNull(coll, "Failed to create HttpParameterCollection");
            Assert.AreEqual(1, coll.Count, "HttpParameterCollection should have found 1 parameter");
            HttpParameter hpd = coll[0];

            Assert.AreEqual("parameter1", hpd.Name, "Name was not set correctly");
            Assert.AreEqual(typeof(int), hpd.Type, "Type was not set correctly");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection can be constructed from output MessagePartDescriptionCollection")]
        public void HttpParameterCollection_Ctor_From_Output_MessagePartDescriptionCollection()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpParameterCollection coll = new HttpParameterCollection(od, isOutputCollection: true);
            Assert.IsNotNull(coll, "Failed to create HttpParameterCollection");
            Assert.AreEqual(1, coll.Count, "HttpParameterCollection should have found 1 parameter");
            HttpParameter hpd = coll[0];
           
            Assert.AreEqual("parameter3a", hpd.Name, "Name was not set correctly");
            Assert.AreEqual(typeof(double), hpd.Type, "Type was not set correctly");
        }

        #endregion Constructor Tests

        #region Update Unsynchronized Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements Clear correctly")]
        public void HttpParameterCollection_Unsynchronized_Implements_Clear()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // Clear
            coll.Clear();
            Assert.AreEqual(0, coll.Count, "Clear failed");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements indexer correctly")]
        public void HttpParameterCollection_Unsynchronized_Indexer()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));
            HttpParameter hpdTemp = null;

            coll.Add(hpd1);
            coll.Add(hpd2);

            // Indexer get
            Assert.AreEqual(2, coll.Count, "Count incorrect");
            Assert.AreSame(hpd1, coll[0], "Indexer[0] incorrect");
            Assert.AreSame(hpd2, coll[1], "Indexer[1] incorrect");

            // Indexer get negative
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdTemp = coll[2]);

            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdTemp = coll[-1]);

            // Indexer set
            coll[1] = hpd3;
            Assert.AreSame(hpd3, coll[1], "Indexer set failed");

            // Indexer set null item
            ExceptionAssert.ThrowsArgumentNull("value", () => coll[0] = null);

            // Indexer set negative
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => coll[5] = hpd2);
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => coll[-1] = hpd2);
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements IndexOf correctly")]
        public void HttpParameterCollection_Unsynchronized_IndexOf()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // IndexOf
            Assert.AreEqual(0, coll.IndexOf(hpd1), "IndexOf[0] incorrect");
            Assert.AreEqual(1, coll.IndexOf(hpd2), "IndexOf[1] incorrect");
            Assert.AreEqual(-1, coll.IndexOf(hpd3), "IndexOf[none] incorrect");

            // IndexOf negative
            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => coll.IndexOf(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements Contains correctly")]
        public void HttpParameterCollection_Unsynchronized_Contains()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // Contains
            Assert.IsTrue(coll.Contains(hpd1), "Contains[0] incorrect");
            Assert.IsTrue(coll.Contains(hpd2), "Contains[1] incorrect");
            Assert.IsFalse(coll.Contains(hpd3), "Contains[none] incorrect");

            // Contains negative
            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => coll.Contains(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements IsReadOnly correctly")]
        public void HttpParameterCollection_Unsynchronized_IsReadOnly()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            Assert.IsFalse(coll.IsReadOnly, "Collection should not be readonly");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements CopyTo correctly")]
        public void HttpParameterCollection_Unsynchronized_CopyTo()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // CopyTo
            HttpParameter[] arr = new HttpParameter[2];
            coll.CopyTo(arr, 0);
            Assert.AreSame(hpd1, arr[0], "CopyTo[0] failed");
            Assert.AreSame(hpd2, arr[1], "CopyTo[1] failed");

            // CopyTo negative tests
            ExceptionAssert.ThrowsArgumentNull("array", () => coll.CopyTo(null, 0));

            ExceptionAssert.ThrowsArgumentOutOfRange("arrayIndex", () => coll.CopyTo(arr, -1));
            ExceptionAssert.ThrowsArgumentOutOfRange("arrayIndex", () => coll.CopyTo(arr, 2));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements Insert correctly")]
        public void HttpParameterCollection_Unsynchronized_Insert()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            // Insert semamtics allow index==Count.  Verify.
            coll.Insert(0, hpd1);
            coll.Insert(1, hpd2);

            // Now really insert between
            coll.Insert(1, hpd3);

            Assert.AreEqual(3, coll.Count, "Insert failed");
            Assert.AreSame(hpd3, coll[1], "Insert went to wrong spot");
            Assert.AreSame(hpd2, coll[2], "Insert did not move items");

            // Insert negative
            ExceptionAssert.ThrowsArgumentNull("item", () => coll.Insert(0, null));

            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => coll.Insert(-1, hpd3));
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => coll.Insert(4, hpd3));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements Remove correctly")]
        public void HttpParameterCollection_Unsynchronized_Remove()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // Remove
            coll.Remove(hpd3);
            Assert.AreEqual(2, coll.Count, "Remove failed");
            Assert.IsFalse(coll.Contains(hpd3), "Remove still shows contains");

            // Remove negative
            Assert.IsFalse(coll.Remove(hpd3), "Redundant remove should have returned false");

            ExceptionAssert.ThrowsArgumentNull("item", () => coll.Remove(null));

        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements RemoveAt correctly")]
        public void HttpParameterCollection_Unsynchronized_RemoveAt()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // RemoveAt
            coll.Add(hpd3);
            Assert.AreEqual(3, coll.Count, "Add failed");
            Assert.IsTrue(coll.Contains(hpd3), "Contains after add failed");
            coll.RemoveAt(2);
            Assert.AreEqual(2, coll.Count, "RemoveAt count failed");
            Assert.IsFalse(coll.Contains(hpd3), "RemoveAt+Contains failed");

            // RemoveAt negative
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => coll.RemoveAt(-1));
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => coll.RemoveAt(3));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from default ctor implements GetEnumerator correctly")]
        public void HttpParameterCollection_Unsynchronized_GetEnumerator()
        {
            HttpParameterCollection coll = new HttpParameterCollection();
            HttpParameter hpd1 = new HttpParameter("First", typeof(string));
            HttpParameter hpd2 = new HttpParameter("Second", typeof(int));
            HttpParameter hpd3 = new HttpParameter("Third", typeof(double));

            coll.Add(hpd1);
            coll.Add(hpd2);

            // GetEnumerator
            IEnumerator<HttpParameter> ie = coll.GetEnumerator();
            Assert.IsNotNull(ie, "GetEnumerator failed");
            object[] items = EnumeratorToArray(ie);
            AssertSame(coll, items, "Generic enumerator");

            // Non-generic GetEnumerator
            IEnumerator iec = ((IEnumerable)coll).GetEnumerator();
            Assert.IsNotNull(iec, "GetEnumerator failed");
            items = EnumeratorToArray(iec);
            AssertSame(coll, items, "Non-generic enumerator");
        }

        #endregion Update Unsynchronized Tests

        #region Update Synchronized with MessagePartDescription Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements Clear correctly")]
        public void HttpParameterCollection_Synchronized_Implements_Clear()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);

            hpdColl.Clear();
            Assert.AreEqual(0, hpdColl.Count, "Clear failed");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements Indexer correctly")]
        public void HttpParameterCollection_Synchronized_Indexer()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            HttpParameter hpdTemp = null;

            // Indexer get (note this verifies HPD indexer redirects to original MPD coll
            Assert.AreEqual(2, hpdColl.Count, "Count incorrect");
            Assert.AreSame(hpd1.MessagePartDescription, hpdColl[0].MessagePartDescription, "Indexer[0] incorrect");
            Assert.AreSame(hpd2.MessagePartDescription, hpdColl[1].MessagePartDescription, "Indexer[1] incorrect");

            // Indexer get negative
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdTemp = hpdColl[2]);
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdTemp = hpdColl[-1]);

            // Indexer set
            hpdColl[1] = hpd3;
            Assert.AreEqual(2, hpdColl.Count, "Index set should have not affected count");
            Assert.AreSame(hpd3.MessagePartDescription, hpdColl[1].MessagePartDescription, "Indexer[1] set incorrect");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements IndexOf correctly")]
        public void HttpParameterCollection_Synchronized_IndexOf()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // IndexOf
            Assert.AreEqual(0, hpdColl.IndexOf(hpd1), "IndexOf[0] incorrect");
            Assert.AreEqual(1, hpdColl.IndexOf(hpd2), "IndexOf[1] incorrect");
            Assert.AreEqual(-1, hpdColl.IndexOf(hpd3), "IndexOf[none] incorrect");

            // IndexOf negative
            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => hpdColl.IndexOf(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements Contains correctly")]
        public void HttpParameterCollection_Synchronized_Implements_Contains()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // Contains
            Assert.IsTrue(hpdColl.Contains(hpd1), "Contains[0] incorrect");
            Assert.IsTrue(hpdColl.Contains(hpd2), "Contains[1] incorrect");
            Assert.IsFalse(hpdColl.Contains(hpd3), "Contains[none] incorrect");

            // Contains negative
            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => hpdColl.Contains(null));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements IsReadOnly correctly")]
        public void HttpParameterCollection_Synchronized_IsReadOnly()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements CopyTo correctly for one-way operations")]
        public void HttpParameterCollection_Synchronized_OneWay_CopyTo()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputOneWay");
            MessagePartDescriptionCollection mpdColl1 = od.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl1.Count, "MessagePartDescriptionCollection should show existing input parameter");

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl1 = new HttpParameterCollection(od, isOutputCollection: false);
            MessagePartDescription mpd1 = mpdColl1[0];
                        
            // CopyTo
            HttpParameter[] arr = new HttpParameter[2];
            hpdColl1.CopyTo(arr, 0);
            hpdColl1.CopyTo(arr, 1);
            Assert.AreSame(mpd1, arr[0].MessagePartDescription, "CopyTo[0] failed");
            Assert.AreSame(mpd1, arr[1].MessagePartDescription, "CopyTo[0] failed");

            HttpParameterCollection hpdColl2 = new HttpParameterCollection(od, isOutputCollection: true);
            hpdColl2.CopyTo(arr, 0);
            Assert.AreSame(arr[0], null, "CopyTo[0] failed, expected to null the clear the contents of the array");
            Assert.AreSame(arr[1], null, "CopyTo[0] failed, expected to null the clear the contents of the array");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements CopyTo correctly")]
        public void HttpParameterCollection_Synchronized_CopyTo()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // CopyTo
            HttpParameter[] arr = new HttpParameter[2];
            hpdColl.CopyTo(arr, 0);
            Assert.AreSame(hpd1.MessagePartDescription, arr[0].MessagePartDescription, "CopyTo[0] failed");
            Assert.AreSame(hpd2.MessagePartDescription, arr[1].MessagePartDescription, "CopyTo[1] failed");

            // CopyTo negative tests
            ExceptionAssert.ThrowsArgumentNull("array", () => hpdColl.CopyTo(null, 0));
            ExceptionAssert.ThrowsArgumentOutOfRange("arrayIndex", () => hpdColl.CopyTo(arr, -1));
            ExceptionAssert.ThrowsArgumentOutOfRange("arrayIndex", () => hpdColl.CopyTo(arr, 2));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements Insert correctly")]
        public void HttpParameterCollection_Synchronized_Insert()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // Insert
            hpdColl.Insert(1, hpd3);
            Assert.AreEqual(3, hpdColl.Count, "Insert failed");
            Assert.AreSame(hpd3.MessagePartDescription, hpdColl[1].MessagePartDescription, "Insert went to wrong spot");
            Assert.AreSame(hpd2.MessagePartDescription, hpdColl[2].MessagePartDescription, "Insert did not move items");

            // Insert negative
            ExceptionAssert.ThrowsArgumentNull("item", () => hpdColl.Insert(0, null));
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdColl.Insert(-1, hpd3));
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdColl.Insert(4, hpd3));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements Remove correctly")]
        public void HttpParameterCollection_Synchronized_Remove()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // Remove
            hpdColl.Remove(hpd3);
            Assert.AreEqual(2, hpdColl.Count, "Remove failed");
            Assert.IsFalse(hpdColl.Contains(hpd3), "Remove still shows contains");

            // Remove negative
            Assert.IsFalse(hpdColl.Remove(hpd3), "Redundant remove should have returned false");

            ExceptionAssert.ThrowsArgumentNull(
                "item",
                () => hpdColl.Remove(null)
                );
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements RemoveAt correctly")]
        public void HttpParameterCollection_Synchronized_RemoveAt()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // RemoveAt
            hpdColl.Add(hpd3);
            Assert.AreEqual(3, hpdColl.Count, "Add failed");
            Assert.IsTrue(hpdColl.Contains(hpd3), "Contains after add failed");
            hpdColl.RemoveAt(2);
            Assert.AreEqual(2, hpdColl.Count, "RemoveAt count failed");
            Assert.IsFalse(hpdColl.Contains(hpd3), "RemoveAt+Contains failed");

            // RemoveAt negative
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdColl.RemoveAt(-1));

            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdColl.RemoveAt(3));
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from MessagePartDescriptionCollection implements GetEnumerator correctly")]
        public void HttpParameterCollection_Synchronized_GetEnumerator()
        {
            OperationDescription od1 = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            OperationDescription od2 = GetOperationDescription(typeof(SimpleOperationsService), "OneInputAndReturnValue");
            MessagePartDescriptionCollection mpdColl = od1.Messages[0].Body.Parts;
            Assert.AreEqual(2, mpdColl.Count, "MessagePartDescriptionCollection should show 2 existing input parameters");

            MessagePartDescriptionCollection mpdColl2 = od2.Messages[0].Body.Parts;
            Assert.AreEqual(1, mpdColl2.Count, "MessagePartDescriptionCollection 2 should show 1 existing input parameters");

            // Pull out individual parts to test synching at item level
            MessagePartDescription mpd1 = mpdColl[0];
            MessagePartDescription mpd2 = mpdColl[1];

            // Use a MPD from a 2nd collection so we can add and remove it
            MessagePartDescription mpd3 = mpdColl2[0];

            // This ctor creates the synchronized form of the collection.   It should immediately reflect
            // the state of the MPD collection
            HttpParameterCollection hpdColl = new HttpParameterCollection(od1, isOutputCollection: false);
            Assert.IsNotNull(hpdColl, "Failed to create HttpParameterCollection");
            Assert.AreEqual(2, hpdColl.Count, "HttpParameterCollection should show 2 existing input parameters");

            // Extension method creates synched version of HPD from MPD's
            HttpParameter hpd1 = mpd1.ToHttpParameter();
            HttpParameter hpd2 = mpd2.ToHttpParameter();

            // Ensure the extension method created HPD's that point to the idential MPD
            Assert.AreEqual(mpd1, hpd1.MessagePartDescription, "HttParameterDescription 1 linked to wrong MessagePartDescription");
            Assert.AreEqual(mpd2, hpd2.MessagePartDescription, "HttParameterDescription 2 linked to wrong MessagePartDescription");

            // Keep one from 2nd collection
            HttpParameter hpd3 = mpd3.ToHttpParameter();

            // GetEnumerator
            IEnumerator<HttpParameter> ie = hpdColl.GetEnumerator();
            object[] items = EnumeratorToArray(ie);
            AssertSame(hpdColl, items, "Generic enumerator");

            // Non-generic GetEnumerator
            IEnumerator iec = ((IEnumerable)hpdColl).GetEnumerator();
            Assert.IsNotNull(iec, "GetEnumerator failed");
            items = EnumeratorToArray(iec);
            AssertSame(hpdColl, items, "Nongeneric enumerator");
        }

        #endregion Update Synchronized with MessagePartDescription Tests

        #region Update Synchronized with incomplete MessagePartDescription Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements Clear correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_Implements_Clear()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            od.Messages.Clear();
            HttpParameterCollection hpdColl = new HttpParameterCollection(od, isOutputCollection: false);
            hpdColl.Clear();
            Assert.AreEqual(0, hpdColl.Count, "Clear failed");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements Indexer correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_Indexer()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            HttpParameterCollection hpdColl = new HttpParameterCollection(od, isOutputCollection: false);
            HttpParameter hpd = hod.InputParameters[0];

            // Zap both inputs and outputs
            MessageDescription mdInput = od.Messages[0];
            MessageDescription mdOutput = od.Messages[1];
            od.Messages.Clear();

            // Verify the HOD sees an empty set
            Assert.AreEqual(0, hod.InputParameters.Count, "Expected zero input parameters");

            // Verify our local collection sees an empty set
            Assert.AreEqual(0, hpdColl.Count, "Expected zero elements in local collection");

            // Expect ArgumentOutOfRangeException using get indexer
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpd = hod.InputParameters[0]);

            // Same exception indexing our local collection
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpd = hpdColl[0]);

            // Expect ArgumentOutOfRangeException using set indexer on InputParameters
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hod.InputParameters[0] = hpd);

            // Same exception setting on our local collection
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdColl[0] = hpd);

            // Add back a real input MessageDescription and verify collections have content again
            od.Messages.Add(mdInput);

            Assert.AreEqual(2, hod.InputParameters.Count, "HOD.InputParameters did not update when added MessageDescription");
            Assert.AreEqual(2, hpdColl.Count, "HODColl.Count did not update when added MessageDescription");

            // The indexer get should work again
            HttpParameter hpd1 = hpdColl[0];
            Assert.IsNotNull(hpd1, "Unexpected null reindexing collection");
            hpd1 = hod.InputParameters[0];
            Assert.IsNotNull(hpd1, "Unexpected null reindexing InputParameters");

            // And so should the setter indexer
            hpdColl[0] = hpd;
            hod.InputParameters[0] = hpd;
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements IndexOf correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_IndexOf()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            HttpParameterCollection hpdColl = new HttpParameterCollection(od, isOutputCollection: false);
            HttpParameter hpd = hod.InputParameters[0];

            Assert.AreEqual(0, hpdColl.IndexOf(hpd), "Prove IndexOf works prior to clearing");

            // Zap both inputs and outputs
            MessageDescription mdInput = od.Messages[0];
            MessageDescription mdOutput = od.Messages[1];
            od.Messages.Clear();

            // Verify the HOD sees an empty set
            Assert.AreEqual(0, hod.InputParameters.Count, "Expected zero input parameters");
            Assert.AreEqual(0, hpdColl.Count, "Expected zero elements in local collection");

            // Verify IndexOf cannot find it in either collection
            Assert.AreEqual(-1, hod.InputParameters.IndexOf(hpd), "InputParameters.IndexOf should not have found it");
            Assert.AreEqual(-1, hpdColl.IndexOf(hpd), "HPDColl.IndexOf should not have found it");

            // Add back a real input MessageDescription and verify collections have content again
            od.Messages.Add(mdInput);

            Assert.AreEqual(0, hod.InputParameters.IndexOf(hpd), "InputParameters.IndexOf should have found it");
            Assert.AreEqual(0, hpdColl.IndexOf(hpd), "HPDColl.IndexOf should have found it");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements Contains correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_Implements_Contains()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            HttpParameterCollection hpdColl = new HttpParameterCollection(od, isOutputCollection: false);
            HttpParameter hpd = hod.InputParameters[0];

            Assert.IsTrue(hpdColl.Contains(hpd), "Prove Contains works prior to clearing");

            // Zap both inputs and outputs
            MessageDescription mdInput = od.Messages[0];
            MessageDescription mdOutput = od.Messages[1];
            od.Messages.Clear();

            // Verify Contains cannot find it in either collection
            Assert.IsFalse(hod.InputParameters.Contains(hpd), "InputParameters.Contains should not have found it");
            Assert.IsFalse(hpdColl.Contains(hpd), "HPDColl.Contains should not have found it");

            // Add back a real input MessageDescription and verify collections have content again
            od.Messages.Add(mdInput);

            Assert.IsTrue(hod.InputParameters.Contains(hpd), "InputParameters.Contains should have found it");
            Assert.IsTrue(hpdColl.Contains(hpd), "HPDColl.Contains should have found it");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements IsReadOnly correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_IsReadOnly()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            od.Messages.Clear();
            Assert.IsFalse(hod.InputParameters.IsReadOnly, "Input should should as not readonly regardless of sync");
            Assert.IsFalse(hod.OutputParameters.IsReadOnly, "Output should should as not readonly regardless of sync");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements CopyTo correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_CopyTo()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "OneInputTwoOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            HttpParameterCollection hpdColl = new HttpParameterCollection(od, isOutputCollection: false);
            HttpParameter hpd = hod.InputParameters[0];

            Assert.IsTrue(hpdColl.Contains(hpd), "Prove Contains works prior to clearing");

            // Zap both inputs and outputs
            MessageDescription mdInput = od.Messages[0];
            MessageDescription mdOutput = od.Messages[1];
            od.Messages.Clear();

            HttpParameter[] arr = new HttpParameter[2];

            // CopyTo should return array with null elements starting from array index 
            hpdColl.CopyTo(arr, 0);
            Assert.AreEqual(null, arr[0]);
            Assert.AreEqual(null, arr[1]);

            od.Messages.Add(mdInput);
            hpdColl.CopyTo(arr, 0);
            Assert.AreEqual(hpd.MessagePartDescription, arr[0].MessagePartDescription, "Copy did not yield expected instance");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements Insert correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_Insert()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();

            // Pull collections into locals
            IList<HttpParameter> hpdCollInput = hod.InputParameters;
            IList<HttpParameter> hpdCollOutput = hod.OutputParameters;

            HttpParameter hpdInput = hpdCollInput[0];
            HttpParameter hpdOutput = hpdCollOutput[0];

            // Zap the Messages collection
            od.Messages.Clear();

            Assert.AreEqual(0, hpdCollInput.Count, "Clearing Messages should have reset input count");
            Assert.AreEqual(0, hpdCollOutput.Count, "Clearing Messages should have reset output count");

            // Inserting into output should autocreate both Messages
            // This also verifies insert where index==Count which is legal
            hpdCollOutput.Insert(0, hpdOutput);

            Assert.AreEqual(1, hpdCollOutput.Count, "Failed to insert output");
            Assert.AreEqual(2, od.Messages.Count, "Failed to autocreate Messages[1]");

            // Should be possible to insert input again too
            hpdCollInput.Insert(0, hpdInput);
            Assert.AreEqual(1, hpdCollInput.Count, "Failed to insert input");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements Remove correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_Remove()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            // Pull collections into locals
            IList<HttpParameter> hpdCollInput = hod.InputParameters;
            IList<HttpParameter> hpdCollOutput = hod.OutputParameters;

            HttpParameter hpdInput = hpdCollInput[0];
            HttpParameter hpdOutput = hpdCollOutput[0];

            // Zap the Messages collection
            od.Messages.Clear();

            Assert.AreEqual(0, hpdCollInput.Count, "Clearing Messages should have reset input count");
            Assert.AreEqual(0, hpdCollOutput.Count, "Clearing Messages should have reset output count");

            // Remove
            bool removed = hpdCollInput.Remove(hpdOutput);
            Assert.IsFalse(removed, "Remove of input should have returned false");
            
            removed = hpdCollOutput.Remove(hpdOutput);
            Assert.IsFalse(removed, "Remove of output should have returned false");

            // Put it back to verify Remove can work after recreate Messages
            hpdCollOutput.Add(hpdOutput);

            Assert.AreEqual(2, od.Messages.Count, "Expected Messages to be autocreated");

            removed = hpdCollOutput.Remove(hpdOutput);
            Assert.IsTrue(removed, "Remove of output after autogen should have returned true");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements RemoveAt correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_RemoveAt()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            // Pull collections into locals
            IList<HttpParameter> hpdCollInput = hod.InputParameters;
            IList<HttpParameter> hpdCollOutput = hod.OutputParameters;

            HttpParameter hpdInput = hpdCollInput[0];
            HttpParameter hpdOutput = hpdCollOutput[0];

            // Zap the Messages collection
            od.Messages.Clear();

            Assert.AreEqual(0, hpdCollInput.Count, "Clearing Messages should have reset input count");
            Assert.AreEqual(0, hpdCollOutput.Count, "Clearing Messages should have reset output count");

            // RemoveAt should throw on empty collections
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdCollInput.RemoveAt(0));
            ExceptionAssert.ThrowsArgumentOutOfRange("index", () => hpdCollOutput.RemoveAt(0));

            // Put it back to verify RemoveAt can work after recreate Messages
            hpdCollOutput.Add(hpdOutput);
            hpdCollInput.Add(hpdInput);

            Assert.AreEqual(2, od.Messages.Count, "Expected Messages to be autocreated");
            Assert.AreEqual(1, hpdCollInput.Count, "Expected input coll count to be 1");
            Assert.AreEqual(1, hpdCollOutput.Count, "Expected output coll count to be 1");

            // RemoveAt should work again
            hpdCollInput.RemoveAt(0);
            hpdCollOutput.RemoveAt(0);

            Assert.AreEqual(0, hpdCollInput.Count, "Expected input coll count to be 0");
            Assert.AreEqual(0, hpdCollOutput.Count, "Expected output coll count to be ");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements generic GetEnumerator correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_Generic_GetEnumerator()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            // Pull collections into locals
            IList<HttpParameter> hpdCollInput = hod.InputParameters;
            IList<HttpParameter> hpdCollOutput = hod.OutputParameters;

            HttpParameter hpdInput = hpdCollInput[0];
            HttpParameter hpdOutput = hpdCollOutput[0];

            // Zap the Messages collection
            od.Messages.Clear();

            int nItems = 0;
            using (IEnumerator<HttpParameter> ie = hpdCollInput.GetEnumerator())
            {
                Assert.IsNotNull(ie, "enumerator should not be null");
                nItems = 0;
                while (ie.MoveNext())
                {
                    ++nItems;
                }
            }
            Assert.AreEqual(0, nItems, "Input enumerator should have been empty");

            using (IEnumerator<HttpParameter> ie = hpdCollOutput.GetEnumerator())
            {
                Assert.IsNotNull(ie, "enumerator should not be null");
                nItems = 0;
                while (ie.MoveNext())
                {
                    ++nItems;
                }
            }
            Assert.AreEqual(0, nItems, "Output enumerator should have been empty");

            // Add back the HPD's and verify enumerator works again
            hpdCollInput.Add(hpdInput);
            hpdCollOutput.Add(hpdOutput);

            using (IEnumerator<HttpParameter> ie = hpdCollInput.GetEnumerator())
            {
                Assert.IsNotNull(ie, "input enumerator should not be null");
                nItems = 0;
                while (ie.MoveNext())
                {
                    ++nItems;
                }
            }
            Assert.AreEqual(1, nItems, "Input enumerator should have had one item");

            using (IEnumerator<HttpParameter> ie = hpdCollOutput.GetEnumerator())
            {
                Assert.IsNotNull(ie, "output enumerator should not be null");
                nItems = 0;
                while (ie.MoveNext())
                {
                    ++nItems;
                }
            }
            Assert.AreEqual(1, nItems, "Output enumerator should have had one item");
         }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("HttpParameterCollection created from incomplete MessagePartDescriptionCollection implements GetEnumerator correctly")]
        public void HttpParameterCollection_Synchronized_Incomplete_GetEnumerator()
        {
            OperationDescription od = GetOperationDescription(typeof(SimpleOperationsService), "TwoInputOneOutputAndReturnValue");
            HttpOperationDescription hod = od.ToHttpOperationDescription();
            // Pull collections into locals
            IList<HttpParameter> hpdCollInput = hod.InputParameters;
            IList<HttpParameter> hpdCollOutput = hod.OutputParameters;

            HttpParameter hpdInput = hpdCollInput[0];
            HttpParameter hpdOutput = hpdCollOutput[0];

            // Zap the Messages collection
            od.Messages.Clear();

            // Empty enumerators should match
            IEnumerator ie = ((IEnumerable)hpdCollInput).GetEnumerator();
            Assert.IsNotNull(ie, "enumerator should not be null");
            object[] items = EnumeratorToArray(ie);
            Assert.AreEqual(0, items.Length, "Expected empty enumerator for input");

            ie = ((IEnumerable)hpdCollOutput).GetEnumerator();
            Assert.IsNotNull(ie, "enumerator should not be null");
            items = EnumeratorToArray(ie);
            Assert.AreEqual(0, items.Length, "Expected empty enumerator for output");

            // Add back the HPD's and verify enumerator works again
            hpdCollInput.Add(hpdInput);
            hpdCollOutput.Add(hpdOutput);

            ie = ((IEnumerable)hpdCollInput).GetEnumerator();
            Assert.IsNotNull(ie, "input enumerator should not be null");
            items = EnumeratorToArray(ie);
            Assert.AreEqual(1, items.Length, "Expected nonempty enumerator for input");
            AssertSame(hpdCollInput, items, "Generic input");

            ie = ((IEnumerable)hpdCollOutput).GetEnumerator();
            Assert.IsNotNull(ie, "output enumerator should not be null");
            items = EnumeratorToArray(ie);
            Assert.AreEqual(1, items.Length, "Expected nonempty enumerator for output");
            AssertSame(hpdCollOutput, items, "Generic input");
        }

        #endregion Update Synchronized with incomplete MessagePartDescription Tests

        #region helper

        private static object[] EnumeratorToArray(IEnumerator ie)
        {
            List<object> result = new List<object>();
            while (ie.MoveNext())
            {
                result.Add(ie.Current);
            }
            return result.ToArray();
        }

        private static void AssertSame(IList<HttpParameter> coll, object[] items, string message)
        {
            Assert.AreEqual(coll.Count, items.Length, message + ": length mismatch");
            for (int i = 0; i < items.Length; ++i)
            {
                HttpParameter hpd1 = coll[i];
                HttpParameter hpd2 = items[i] as HttpParameter;
                Assert.IsNotNull(hpd1, message + ": null in collection");
                Assert.IsNotNull(hpd2, message + ": null in items");
                Assert.AreSame(hpd1.MessagePartDescription, hpd2.MessagePartDescription, message + ": different MessagePartDescriptions");
            }
        }

        public static OperationDescription GetOperationDescription(Type contractType, string methodName)
        {
            ContractDescription cd = ContractDescription.GetContract(contractType);
            OperationDescription od = cd.Operations.FirstOrDefault(o => o.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            Assert.IsNotNull(od, "Failed to get operation description for " + methodName);
            return od;
        }
        #endregion helper
    }
}
