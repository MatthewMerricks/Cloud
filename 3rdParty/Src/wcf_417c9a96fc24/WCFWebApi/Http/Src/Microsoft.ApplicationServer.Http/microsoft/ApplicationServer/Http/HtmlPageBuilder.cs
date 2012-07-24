// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Http
{
    using System;
    using System.Linq;
    using System.Web;
    using System.Xml.Linq;
    using Microsoft.ApplicationServer.Common;

    internal class HtmlPageBuilder
    {
        internal const string HelpPageHtml = "BODY { color: #000000; background-color: white; font-family: Verdana; margin-left: 0px; margin-top: 0px; } #content { margin-left: 30px; font-size: .70em; padding-bottom: 2em; } A:link { color: #336699; font-weight: bold; text-decoration: underline; } A:visited { color: #6699cc; font-weight: bold; text-decoration: underline; } A:active { color: #336699; font-weight: bold; text-decoration: underline; } .heading1 { background-color: #003366; border-bottom: #336699 6px solid; color: #ffffff; font-family: Tahoma; font-size: 26px; font-weight: normal;margin: 0em 0em 10px -20px; padding-bottom: 8px; padding-left: 30px;padding-top: 16px;} pre { font-size:small; background-color: #e5e5cc; padding: 5px; font-family: Courier New; margin-top: 0px; border: 1px #f0f0e0 solid; white-space: pre-wrap; white-space: -pre-wrap; word-wrap: break-word; } table { border-collapse: collapse; border-spacing: 0px; font-family: Verdana;} table th { border-right: 2px white solid; border-bottom: 2px white solid; font-weight: bold; background-color: #cecf9c;} table td { border-right: 2px white solid; border-bottom: 2px white solid; background-color: #e5e5cc;}";
        internal const string HtmlDocumentName = "html";
        internal const string HtmlDocumentProductId = "-//W3C//DTD XHTML 1.0 Transitional//EN";
        internal const string HtmlDocumentSystemId = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd";
        internal const string HtmlHtmlElementName = "{http://www.w3.org/1999/xhtml}html";
        internal const string HtmlHeadElementName = "{http://www.w3.org/1999/xhtml}head";
        internal const string HtmlTitleElementName = "{http://www.w3.org/1999/xhtml}title";
        internal const string HtmlBodyElementName = "{http://www.w3.org/1999/xhtml}body";
        internal const string HtmlStyleElementName = "{http://www.w3.org/1999/xhtml}style";
        internal const string HtmlBrElementName = "{http://www.w3.org/1999/xhtml}br";
        internal const string HtmlPElementName = "{http://www.w3.org/1999/xhtml}p";
        internal const string HtmlTableElementName = "{http://www.w3.org/1999/xhtml}table";
        internal const string HtmlTrElementName = "{http://www.w3.org/1999/xhtml}tr";
        internal const string HtmlThElementName = "{http://www.w3.org/1999/xhtml}th";
        internal const string HtmlTdElementName = "{http://www.w3.org/1999/xhtml}td";
        internal const string HtmlDivElementName = "{http://www.w3.org/1999/xhtml}div";
        internal const string HtmlAElementName = "{http://www.w3.org/1999/xhtml}a";
        internal const string HtmlPreElementName = "{http://www.w3.org/1999/xhtml}pre";
        internal const string HtmlClassAttributeName = "class";
        internal const string HtmlTitleAttributeName = "title";
        internal const string HtmlHrefAttributeName = "href";
        internal const string HtmlRelAttributeName = "rel";
        internal const string HtmlIdAttributeName = "id";
        internal const string HtmlNameAttributeName = "name";
        internal const string HtmlRowspanAttributeName = "rowspan";
        internal const string HtmlHeading1Class = "heading1";
        internal const string HtmlContentClass = "content";

        internal const string HtmlRequestXmlId = "request-xml";
        internal const string HtmlRequestJsonId = "request-json";
        internal const string HtmlRequestSchemaId = "request-schema";
        internal const string HtmlResponseXmlId = "response-xml";
        internal const string HtmlResponseJsonId = "response-json";
        internal const string HtmlResponseSchemaId = "response-schema";
        internal const string HtmlOperationClass = "operation";

        internal static XDocument CreateMethodNotAllowedPage(Uri helpUri)
        {
            XDocument document = CreateBaseDocument(SR.HtmlPageTitleText);

            XElement preElement = new XElement(
                HtmlPElementName, 
                new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                SR.HtmlPageTitleText);

            XElement div = new XElement(
                HtmlDivElementName, 
                new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                preElement);

            if (helpUri == null)
            {
                div.Add(new XElement(HtmlPElementName, SR.HtmlPageMethodNotAllowed));
            }
            else
            {
                div.Add(XElement.Parse(SR.HtmlPageMethodNotAllowedWithLink(HttpUtility.HtmlEncode(helpUri.AbsoluteUri))));
            }

            document.Descendants(HtmlBodyElementName).First().Add(div);
            return document;
        }

        internal static XDocument CreateServerErrorPage(Uri helpUri, Exception error)
        {
            XDocument document = CreateBaseDocument(SR.HtmlPageRequestErrorTitle);

            XElement preElement = new XElement(
                HtmlPElementName, 
                new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                SR.HtmlPageRequestErrorTitle);

            XElement div = new XElement(
                HtmlDivElementName, 
                new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                preElement);

            if (helpUri == null)
            {
                if (error != null)
                {
                    div.Add(new XElement(HtmlPElementName, SR.HtmlPageServerErrorProcessingRequestWithDetails(error.Message)));
                    div.Add(new XElement(HtmlPElementName, error.StackTrace ?? String.Empty));
                }
                else
                {
                    div.Add(new XElement(HtmlPElementName, SR.HtmlPageServerErrorProcessingRequest));
                }
            }
            else
            {
                string encodedHelpLink = HttpUtility.HtmlEncode(helpUri.AbsoluteUri);
                if (error != null)
                {
                    div.Add(XElement.Parse(SR.HtmlPageServerErrorProcessingRequestWithDetailsAndLink(encodedHelpLink, error.Message)));
                    div.Add(new XElement(HtmlPElementName, error.StackTrace ?? String.Empty));
                }
                else
                {
                    div.Add(XElement.Parse(SR.HtmlPageServerErrorProcessingRequestWithLink(encodedHelpLink)));
                }
            }

            document.Descendants(HtmlBodyElementName).First().Add(div);
            return document;
        }

        internal static XDocument CreateEndpointNotFound(Uri helpUri)
        {
            XDocument document = CreateBaseDocument(SR.HtmlPageTitleText);

            XElement preElement = new XElement(
                HtmlPElementName, 
                new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                SR.HtmlPageTitleText);

            XElement div = new XElement(
                HtmlDivElementName, 
                new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                preElement);

            if (helpUri == null)
            {
                div.Add(new XElement(HtmlPElementName, SR.HtmlPageEndpointNotFound));
            }
            else
            {
                div.Add(XElement.Parse(SR.HtmlPageEndpointNotFoundWithLink(HttpUtility.HtmlEncode(helpUri.AbsoluteUri))));
            }

            document.Descendants(HtmlBodyElementName).First().Add(div);
            return document;
        }

        internal static XDocument CreateTransferRedirectPage(string originalTo, string newLocation)
        {
            Fx.Assert(!string.IsNullOrWhiteSpace(originalTo), "The 'originalTo' parameter should not be null, empty string or whitespace.");
            Fx.Assert(!string.IsNullOrWhiteSpace(newLocation), "The 'newLocation' parameter should not be null, empty string or whitespace.");

            XDocument document = CreateBaseDocument(SR.HtmlPageTitleText);

            XElement preElement = new XElement(
                HtmlPElementName, 
                new XAttribute(HtmlClassAttributeName, HtmlHeading1Class),
                SR.HtmlPageTitleText);

            XElement div = new XElement(
                HtmlDivElementName, 
                new XAttribute(HtmlIdAttributeName, HtmlContentClass),
                preElement,
                XElement.Parse(SR.HtmlPageRedirect(HttpUtility.HtmlEncode(originalTo), HttpUtility.HtmlEncode(newLocation))));

            document.Descendants(HtmlBodyElementName).First().Add(div);
            return document;
        }

        internal static XDocument CreateBaseDocument(string title)
        {
            Fx.Assert(!string.IsNullOrWhiteSpace(title), "The 'title' parameter should not be null, empty string or whitespace.");

            XElement headElement = new XElement(
                HtmlHeadElementName,
                new XElement(HtmlTitleElementName, title),
                new XElement(HtmlStyleElementName, HelpPageHtml));

            XElement htmlElement = new XElement(
                    HtmlHtmlElementName,
                    headElement,
                    new XElement(HtmlBodyElementName));

            return new XDocument(
                new XDocumentType(HtmlDocumentName, HtmlDocumentProductId, HtmlDocumentSystemId, null),
                htmlElement);
        }
    }
}
