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
    public class ParsedMediaTypeHeadeValueTests
    {
        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("MediaTypeHeaderValue ensures only valid media types are constructed.")]
        public void MediaTypeHeaderValue_Ensures_Valid_MediaType()
        {
            string[] invalidMediaTypes = new string[] { "", " ", "\n", "\t", "text", "text/", "text\\", "\\", "//", "text/[", "text/ ", " text/", " text/ ", "text\\ ", " text\\", " text\\ ", "text\\xml", "text//xml" };

            foreach (string invalidMediaType in invalidMediaTypes)
            {
                ExceptionAssert.Throws<Exception>(
                    string.Format("The invalid media-type '{0}' should have caused the MediaTypeHeaderValue constructor to throw.", invalidMediaType),
                    null,
                    () => new MediaTypeHeaderValue(invalidMediaType));
            }
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.Type returns the media type.")]
        public void Type_Returns_Just_The_Type()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("text", parsedMediaType.Type, "ParsedMediaTypeHeadeValue.Type should have returned 'text'.");

            mediaType = new MediaTypeHeaderValue("text/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("text", parsedMediaType.Type, "ParsedMediaTypeHeadeValue.Type should have returned 'text'.");

            mediaType = new MediaTypeHeaderValue("*/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("*", parsedMediaType.Type, "ParsedMediaTypeHeadeValue.Type should have returned '*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.SubType returns the media sub-type.")]
        public void SubType_Returns_Just_The_Sub_Type()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/xml");
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("xml", parsedMediaType.SubType, "ParsedMediaTypeHeadeValue.SubType should have returned 'xml'.");

            mediaType = new MediaTypeHeaderValue("text/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("*", parsedMediaType.SubType, "ParsedMediaTypeHeadeValue.SubType should have returned '*'.");

            mediaType = new MediaTypeHeaderValue("*/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("*", parsedMediaType.SubType, "ParsedMediaTypeHeadeValue.SubType should have returned '*'.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.IsSubTypeMediaRange returns true for media ranges.")]
        public void IsSubTypeMediaRange_Returns_True_For_Media_Ranges()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/*");
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsTrue(parsedMediaType.IsSubTypeMediaRange, "ParsedMediaTypeHeadeValue.IsSubTypeMediaRange should have returned true.");

            mediaType = new MediaTypeHeaderValue("*/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsTrue(parsedMediaType.IsSubTypeMediaRange, "ParsedMediaTypeHeadeValue.IsSubTypeMediaRange should have returned true.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.IsAllMediaRange returns true only when both the type and subtype are wildcard characters.")]
        public void IsAllMediaRange_Returns_True_Only_When_Type_And_SubType_Are_Wildcards()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/*");
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsFalse(parsedMediaType.IsAllMediaRange, "ParsedMediaTypeHeadeValue.IsAllMediaRange should have returned false.");

            mediaType = new MediaTypeHeaderValue("*/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsTrue(parsedMediaType.IsAllMediaRange, "ParsedMediaTypeHeadeValue.IsAllMediaRange should have returned true.");

            mediaType = new MediaTypeHeaderValue("*/xml");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsFalse(parsedMediaType.IsAllMediaRange, "ParsedMediaTypeHeadeValue.IsAllMediaRange should have returned false.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.QualityFactor always returns 1.0 for MediaTypeHeaderValue.")]
        public void QualityFactor_Returns_1_For_MediaTypeHeaderValue()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("text/*");
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(1.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 1.0.");

            mediaType = new MediaTypeHeaderValue("*/*");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(1.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 1.0.");

            mediaType = new MediaTypeHeaderValue("application/xml");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(1.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 1.0.");

            mediaType = new MediaTypeHeaderValue("application/xml");
            mediaType.Parameters.Add(new NameValueHeaderValue("q", "0.5"));
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(1.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 1.0.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.QualityFactor returns q value given by MediaTypeWithQualityHeaderValue.")]
        public void QualityFactor_Returns_Q_Value_For_MediaTypeWithQualityHeaderValue()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeWithQualityHeaderValue("text/*", 0.5);
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(0.5, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 0.5.");

            mediaType = new MediaTypeWithQualityHeaderValue("*/*", 0.0);
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(0.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 0.0.");

            mediaType = new MediaTypeWithQualityHeaderValue("application/xml", 1.0);
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(1.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 1.0.");

            mediaType = new MediaTypeWithQualityHeaderValue("application/xml");
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(1.0, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 1.0.");

            mediaType = new MediaTypeWithQualityHeaderValue("application/xml");
            mediaType.Parameters.Add(new NameValueHeaderValue("q", "0.5"));
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual(0.5, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 0.5.");

            MediaTypeWithQualityHeaderValue mediaTypeWithQuality = new MediaTypeWithQualityHeaderValue("application/xml");
            mediaTypeWithQuality.Quality = 0.2;
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaTypeWithQuality);
            Assert.AreEqual(0.2, parsedMediaType.QualityFactor, "ParsedMediaTypeHeadeValue.QualityFactor should have returned 0.2.");
        }

        [TestMethod]
        [TestCategory("CIT")]
        [Timeout(TimeoutConstant.DefaultTimeout)]
        [Owner("")]
        [Description("ParsedMediaTypeHeadeValue.CharSet is just the value of the CharSet from the MediaTypeHeaderValue.")]
        public void CharSet_Is_CharSet_Of_MediaTypeHeaderValue()
        {
            MediaTypeHeaderValue mediaType = new MediaTypeHeaderValue("application/*");
            ParsedMediaTypeHeaderValue parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsNull(parsedMediaType.CharSet, "ParsedMediaTypeHeadeValue.CharSet should have returned null.");

            mediaType = new MediaTypeHeaderValue("application/*");
            mediaType.CharSet = "";
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.IsNull(parsedMediaType.CharSet, "ParsedMediaTypeHeadeValue.CharSet should have returned null.");

            mediaType = new MediaTypeHeaderValue("application/xml");
            mediaType.CharSet = "someCharSet";
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("someCharSet", parsedMediaType.CharSet, "ParsedMediaTypeHeadeValue.CharSet should have returned 'someCharSet'.");

            mediaType = new MediaTypeHeaderValue("text/xml");
            mediaType.CharSet = "someCharSet";
            parsedMediaType = new ParsedMediaTypeHeaderValue(mediaType);
            Assert.AreEqual("someCharSet", parsedMediaType.CharSet, "ParsedMediaTypeHeadeValue.CharSet should have returned 'someCharSet'.");
        }
    }
}
