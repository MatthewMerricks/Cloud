// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Headers;
    using System.ServiceModel.Dispatcher;
    using Microsoft.ApplicationServer.Http.Dispatcher;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationServer.Common.Test;

    [TestClass]
    public class MediaTypeHeadeValueExtensionMethodsTests
    {
        #region IsMediaRange Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsMediaRange returns true for media ranges.")]
        public void IsMediaRange_Returns_True_For_Media_Ranges()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/*");
            Assert.IsTrue(mediaType.IsMediaRange(), "MediaTypeHeadeValueExtensionMethods.IsMediaRange should have returned true for 'text/*'.");

            mediaType = new MediaTypeHeaderValue("application/*");
            mediaType.CharSet = "ISO-8859-1";
            Assert.IsTrue(mediaType.IsMediaRange(), "MediaTypeHeadeValueExtensionMethods.IsMediaRange should have returned true for 'application/*'.");

            mediaType = new MediaTypeHeaderValue("someType/*");
            Assert.IsTrue(mediaType.IsMediaRange(), "MediaTypeHeadeValueExtensionMethods.IsMediaRange should have returned true for 'someType/*'.");

            mediaType = new MediaTypeHeaderValue("*/*");
            Assert.IsTrue(mediaType.IsMediaRange(), "MediaTypeHeadeValueExtensionMethods.IsMediaRange should have returned true for '*/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsMediaRange returns false for non-media ranges.")]
        public void IsMediaRange_Returns_False_For_Non_Media_Ranges()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            Assert.IsFalse(mediaType.IsMediaRange(), "MediaTypeHeadeValueExtensionMethods.IsMediaRange should have returned false for 'text/xml'.");

            mediaType = new MediaTypeHeaderValue("*/someSubType");
            Assert.IsFalse(mediaType.IsMediaRange(), "MediaTypeHeadeValueExtensionMethods.IsMediaRange should have returned true for '*/someSubType'.");
        }

        #endregion IsMediaRange Tests

        #region IsMediaRange Tests

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true for media ranges.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Ranges()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'text/*'.");

            mediaRange = new MediaTypeHeaderValue("*/*");
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for '*/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true for media ranges regardless of case.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Ranges_Regardless_Of_Case()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("Text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("texT/xml");
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'text/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true when the media type is equaivalent to the media range.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Types_Equaivalent_To_The_Media_Range()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("application/xml");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'application/xml'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true when the media type is a media range equaivalent to the given media range.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Types_That_Are_Equaivalent_Media_Ranges()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/*");
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'text/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true when the media type is a media range more specific than the given media range.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Types_More_Specific_Than_The_Media_Range()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("*/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/*");
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for '*/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true when a charset is given.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Types_With_Charset()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            mediaType.CharSet = "US-ASCII";
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'text/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true when the same charset is given for both the media type and the media range.")]
        public void IsWithinMediaRange_Returns_True_For_Media_Types_With_Charset_And_Media_Ranges_With_Same_Charset()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            mediaType.CharSet = "US-ASCII";
            mediaRange.CharSet = "US-ASCII";
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'text/*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange returns true regardless if the media range has a charset.")]
        public void IsWithinMediaRange_Returns_True_Regardless_Of_Media_Ranges_With_Charset()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            mediaRange.CharSet = "US-ASCII";
            Assert.IsTrue(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned true for 'text/*' even if the media range has a charset.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsMediaRange returns false when the media type and media range have different types.")]
        public void IsWithinMediaRange_Returns_False_When_Type_Is_Different()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("text/*");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");
            Assert.IsFalse(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned false for 'text/*' because the media type is 'application/xml'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsMediaRange returns false when the media type and media range have different sub types.")]
        public void IsWithinMediaRange_Returns_False_When_SubType_Is_Different()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("application/json");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");
            Assert.IsFalse(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned false because of the different sub types.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeadeValueExtensionMethods.IsMediaRange returns false when the media type and media range have different charsets.")]
        public void IsWithinMediaRange_Returns_False_When_Charset_Is_Different()
        {
            MediaTypeHeaderValue mediaRange = new MediaTypeHeaderValue("application/xml");
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/xml");
            mediaType.CharSet = "US-ASCII";
            mediaRange.CharSet = "OtherCharSet";
            Assert.IsFalse(mediaType.IsWithinMediaRange(mediaRange), "MediaTypeHeadeValueExtensionMethods.IsWithinMediaRange should have returned false because of the different charsets.");
        }

        #endregion IsMediaRange Tests

    }
}
