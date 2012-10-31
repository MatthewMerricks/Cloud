using System;
using System.Collections.Generic;
using System.Text;
// RKS using System.Web;
using System.Net;
using System.Security.Cryptography;

namespace Microsoft.WebSolutionsPlatform.Event
{
    /// <summary>
    /// This class is meant to correspond to the UserAgent property for HTTP classes.
    /// </summary>
	public class WebpageEvent : Event
	{
		private IPAddress userHostAddress;
		/// <summary>
		/// Client's IP address
		/// </summary>
		public IPAddress UserHostAddress 
		{
			get
			{
				return userHostAddress;
			}
			set
			{
				userHostAddress = value;
			}
		}

		private Uri uriStem;
		/// <summary>
		/// Uri stem
		/// </summary>
		public Uri UriStem
		{
			get
			{
				return uriStem;
			}
			set
			{
				uriStem = value;
			}
		}

		private Uri logicalUri;
		/// <summary>
		/// Logical Uri queried
		/// </summary>
		public Uri LogicalUri
		{
			get
			{
				return logicalUri;
			}
			set
			{
				logicalUri = value;
			}
		}

		private Uri uriQuery;
		/// <summary>
		/// Uri queried
		/// </summary>
		public Uri UriQuery
		{
			get
			{
				return uriQuery;
			}
			set
			{
				uriQuery = value;
			}
		}

		private Uri urlReferrer;
		/// <summary>
		/// URL of the client's previous request that linked to the current URL
		/// </summary>
		public Uri UrlReferrer
		{
			get
			{
				return urlReferrer;
			}
			set
			{
				urlReferrer = value;
			}
		}

		private string urlReferrerDomain;
		/// <summary>
		/// Referring domain
		/// </summary>
		public string UrlReferrerDomain
		{
			get
			{
				return urlReferrerDomain;
			}
			set
			{
				urlReferrerDomain = value;
			}
		}

		private string hostDomain;
		/// <summary>
		/// Host domain
		/// </summary>
		public string HostDomain
		{
			get
			{
				return hostDomain;
			}
			set
			{
				hostDomain = value;
			}
		}

		private int statusCode;
		/// <summary>
		/// Status code
		/// </summary>
		public int StatusCode
		{
			get
			{
				return statusCode;
			}
			set
			{
				statusCode = value;
			}
		}

		private string virtualRoot;
		/// <summary>
		/// Virtual root
		/// </summary>
		public string VirtualRoot
		{
			get
			{
				return virtualRoot;
			}
			set
			{
				virtualRoot = value;
			}
		}

		private string subDirectory;
		/// <summary>
		/// Sub-directory of virtual root
		/// </summary>
		public string SubDirectory
		{
			get
			{
				return subDirectory;
			}
			set
			{
				subDirectory = value;
			}
		}

		private string ext;
		/// <summary>
		/// Ext
		/// </summary>
		public string Ext
		{
			get
			{
				return ext;
			}
			set
			{
				ext = value;
			}
		}

		private string source;
		/// <summary>
		/// Source
		/// </summary>
		public string Source
		{
			get
			{
				return source;
			}
			set
			{
				source = value;
			}
		}

		private string requestType;
		/// <summary>
		/// RequestType
		/// </summary>
		public string RequestType
		{
			get
			{
				return requestType;
			}
			set
			{
				requestType = value;
			}
		}

        // RKS System.Web is not available in .Net 4.0 Client Profile.  HttpRequest changed to String.
		private String request;
		/// <summary>
		/// User request
		/// </summary>
		public String Request
		{
			get
			{
				return request;
			}
			set
			{
				request = value;
			}
		}

		private string userAgent;
		/// <summary>
		/// User agent
		/// </summary>
		public string UserAgent
		{
			get
			{
				return userAgent;
			}
			set
			{
				userAgent = value;
			}
		}

		private bool activeXControls;
		/// <summary>
		/// Indicates whether the browser supports ActiveX controls
		/// </summary>
		public bool ActiveXControls
		{
			get
			{
				return activeXControls;
			}
			set
			{
				activeXControls = value;
			}
		}

		private bool aol;
		/// <summary>
		/// Indicates whether the client is an America Online (Aol) browser
		/// </summary>
		public bool Aol
		{
			get
			{
				return aol;
			}
			set
			{
				aol = value;
			}
		}

		private bool backgroundSounds;
		/// <summary>
		/// Indicates whether the browser supports playing background sounds using the bgsounds HTML element
		/// </summary>
		public bool BackgroundSounds
		{
			get
			{
				return backgroundSounds;
			}
			set
			{
				backgroundSounds = value;
			}
		}

		private bool beta;
		/// <summary>
		/// Indicates whether the browser is a beta version
		/// </summary>
		public bool Beta
		{
			get
			{
				return beta;
			}
			set
			{
				beta = value;
			}
		}

		private string browser;
		/// <summary>
		/// Browser string (if any) that was sent by the browser in the User-Agent request header
		/// </summary>
		public string Browser
		{
			get
			{
				return browser;
			}
			set
			{
				browser = value;
			}
		}

		private bool cdf;
		/// <summary>
		/// Value indicating whether the browser supports Channel Definition Format (Cdf) for webcasting
		/// </summary>
		public bool Cdf
		{
			get
			{
				return cdf;
			}
			set
			{
				cdf = value;
			}
		}

		private Version clrVersion;
		/// <summary>
		/// Version of the .NET Framework that is installed on the client
		/// </summary>
		public Version ClrVersion
		{
			get
			{
				return clrVersion;
			}
			set
			{
				clrVersion = value;
			}
		}

		private bool cookies;
		/// <summary>
		/// Value indicating whether the browser supports cookies
		/// </summary>
		public bool Cookies
		{
			get
			{
				return cookies;
			}
			set
			{
				cookies = value;
			}
		}

		private bool crawler;
		/// <summary>
		/// Value indicating whether the browser is a search engine Web crawler
		/// </summary>
		public bool Crawler
		{
			get
			{
				return crawler;
			}
			set
			{
				crawler = value;
			}
		}

		private Version ecmaScriptVersion;
		/// <summary>
		/// Version number of ECMAScript that the browser supports
		/// </summary>
		public Version EcmaScriptVersion
		{
			get
			{
				return ecmaScriptVersion;
			}
			set
			{
				ecmaScriptVersion = value;
			}
		}

		private bool frames;
		/// <summary>
		/// Value indicating whether the browser supports HTML frames
		/// </summary>
		public bool Frames
		{
			get
			{
				return frames;
			}
			set
			{
				frames = value;
			}
		}

		private bool javaApplets;
		/// <summary>
		/// Value indicating whether the browser supports Java
		/// </summary>
		public bool JavaApplets
		{
			get
			{
				return javaApplets;
			}
			set
			{
				javaApplets = value;
			}
		}

		private int majorVersion;
		/// <summary>
		/// Major (integer) version number of the browser
		/// </summary>
		public int MajorVersion
		{
			get
			{
				return majorVersion;
			}
			set
			{
				majorVersion = value;
			}
		}

		private double minorVersion;
		/// <summary>
		/// Minor (that is, decimal) version number of the browser
		/// </summary>
		public double MinorVersion
		{
			get
			{
				return minorVersion;
			}
			set
			{
				minorVersion = value;
			}
		}

		private Version mSDomVersion;
		/// <summary>
		/// Version of Microsoft HTML (MSHTML) Document Object Model (DOM) that the browser supports
		/// </summary>
		public Version MSDomVersion
		{
			get
			{
				return mSDomVersion;
			}
			set
			{
				mSDomVersion = value;
			}
		}

		private string platform;
		/// <summary>
		/// Name of the platform that the client uses, if it is known
		/// </summary>
		public string Platform
		{
			get
			{
				return platform;
			}
			set
			{
				platform = value;
			}
		}

		private bool tables;
		/// <summary>
		/// Value indicating whether the browser supports HTML table elements
		/// </summary>
		public bool Tables
		{
			get
			{
				return tables;
			}
			set
			{
				tables = value;
			}
		}

		private string type;
		/// <summary>
		/// Name and major (integer) version number of the browser
		/// </summary>
		public string Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}

		private bool vBScript;
		/// <summary>
		/// Value indicating whether the browser supports Visual Basic Scripting edition (VBScript)
		/// </summary>
		public bool VBScript
		{
			get
			{
				return vBScript;
			}
			set
			{
				vBScript = value;
			}
		}

		private string version;
		/// <summary>
		/// Full (integer and decimal) version number of the browser
		/// </summary>
		public string Version
		{
			get
			{
				return version;
			}
			set
			{
				version = value;
			}
		}

		private Version w3CDomVersion;
		/// <summary>
		/// Version of the World Wide Web Consortium (W3C) XML Document Object Model (DOM) 
		/// that the browser supports
		/// </summary>
		public Version W3CDomVersion
		{
			get
			{
				return w3CDomVersion;
			}
			set
			{
				w3CDomVersion = value;
			}
		}

		private bool win16;
		/// <summary>
		/// Value indicating whether the client is a Win16-based computer
		/// </summary>
		public bool Win16
		{
			get
			{
				return win16;
			}
			set
			{
				win16 = value;
			}
		}

		private bool win32;
		/// <summary>
		/// Value indicating whether the client is a Win32-based computer
		/// </summary>
		public bool Win32
		{
			get
			{
				return win32;
			}
			set
			{
				win32 = value;
			}
		}

		private Guid anonymousId;
		/// <summary>
        /// AnonymousId
		/// </summary>
		public Guid AnonymousId
		{
			get
			{
                return anonymousId;
			}
			set
			{
                anonymousId = value;
			}
		}

		private string sourceServer;
		/// <summary>
		/// SourceServer
		/// </summary>
		public string SourceServer
		{
			get
			{
				return sourceServer;
			}
			set
			{
				sourceServer = value;
			}
		}

		private MD5CryptoServiceProvider uriHash;
		/// <summary>
		/// UaBackgroundSounds
		/// </summary>
		public MD5CryptoServiceProvider UriHash
		{
			get
			{
				return uriHash;
			}
			set
			{
				uriHash = value;
			}
		}

		/// <summary>
		/// Base constructor to create a new web page event
		/// </summary>
        public WebpageEvent() :
            base()
		{
            EventType = new Guid(@"78422526-7B21-4559-8B9A-BC551B46AE34");
            Request = null;
		}

        /// <summary>
        /// Base constructor to create a new web page event from a serialized event
        /// </summary>
        /// <param name="serializationData">Serialized event buffer</param>
        public WebpageEvent(byte[] serializationData) : 
            base(serializationData)
        {
            EventType = new Guid(@"78422526-7B21-4559-8B9A-BC551B46AE34");
        }

		/// <summary>
		/// Used for event serialization.
		/// </summary>
		/// <param name="buffer">SerializationData object passed to store serialized object</param>
		public override void GetObjectData( WspBuffer buffer )
		{
			buffer.AddElement(@"ActiveXControls", activeXControls);
			buffer.AddElement(@"AnonymousId", anonymousId);
			buffer.AddElement(@"Aol", aol);
			buffer.AddElement(@"BackgroundSounds", backgroundSounds);
			buffer.AddElement(@"Beta", beta);
			buffer.AddElement(@"Browser", browser);
			buffer.AddElement(@"Cdf", cdf);
			buffer.AddElement(@"ClrVersion", clrVersion);
			buffer.AddElement(@"Cookies", cookies);
			buffer.AddElement(@"Crawler", crawler);
			buffer.AddElement(@"EcmaScriptVersion", ecmaScriptVersion);
			buffer.AddElement(@"Ext", ext);
			buffer.AddElement(@"Frames", frames);
			buffer.AddElement(@"HostDomain", hostDomain);
			buffer.AddElement(@"JavaApplets", javaApplets);
			buffer.AddElement(@"LogicalUri", logicalUri);
			buffer.AddElement(@"MajorVersion", majorVersion);
			buffer.AddElement(@"MinorVersion", minorVersion);
			buffer.AddElement(@"MSDomVersion", mSDomVersion);
			buffer.AddElement(@"Platform", platform);
			buffer.AddElement(@"RequestType", requestType);
			buffer.AddElement(@"Source", source);
			buffer.AddElement(@"SourceServer", sourceServer);
			buffer.AddElement(@"StatusCode", statusCode);
			buffer.AddElement(@"SubDirectory", subDirectory);
			buffer.AddElement(@"Tables", tables);
			buffer.AddElement(@"Type", type);

			if(uriHash != null && uriHash.InputBlockSize > 1)
			{
				buffer.AddElement(@"UriHash", uriHash.Hash);
			}
			else
			{
				buffer.AddElement(@"UriHash", new byte[] { });
			}

			buffer.AddElement(@"UriQuery", uriQuery);
			buffer.AddElement(@"UriStem", uriStem);
			buffer.AddElement(@"UrlReferrer", urlReferrer);
			buffer.AddElement(@"UrlReferrerDomain", urlReferrerDomain);
			buffer.AddElement(@"UserAgent", userAgent);
			buffer.AddElement(@"UserHostAddress", userHostAddress);
			buffer.AddElement(@"VBScript", vBScript);
			buffer.AddElement(@"Version", version);
			buffer.AddElement(@"VirtualRoot", virtualRoot);
			buffer.AddElement(@"W3CDomVersion", w3CDomVersion);
			buffer.AddElement(@"Win16", win16);
			buffer.AddElement(@"Win32", win32);
		}

        /// <summary>
        /// Method to set properties on WebPageEvent object.
        /// </summary>
        /// <param name="elementName">Property name to be set</param>
        /// <param name="elementValue">Value object</param>
        /// <returns>true if success and false if failed</returns>
        public override bool SetElement(string elementName, object elementValue)
        {
            bool rc = true;

            switch (elementName)
            {
                case @"ActiveXControls":
                    ActiveXControls = (bool) elementValue;
                    break;

                case @"AnonymousId":
                    AnonymousId = (Guid)elementValue;
                    break;

                case @"Aol":
                    Aol = (bool)elementValue;
                    break;

                case @"BackgroundSounds":
                    BackgroundSounds = (bool)elementValue;
                    break;

                case @"Beta":
                    Beta = (bool)elementValue;
                    break;

                case @"Browser":
                    Browser = (string)elementValue;
                    break;

                case @"Cdf":
                    Cdf = (bool)elementValue;
                    break;

                case @"ClrVersion":
                    ClrVersion = (Version)elementValue;
                    break;

                case @"Cookies":
                    Cookies = (bool)elementValue;
                    break;

                case @"Crawler":
                    Crawler = (bool)elementValue;
                    break;

                case @"EcmaScriptVersion":
                    EcmaScriptVersion = (Version)elementValue;
                    break;

                case @"Ext":
                    Ext = (string)elementValue;
                    break;

                case @"Frames":
                    Frames = (bool)elementValue;
                    break;

                case @"HostDomain":
                    HostDomain = (string)elementValue;
                    break;

                case @"JavaApplets":
                    JavaApplets = (bool)elementValue;
                    break;

                case @"LogicalUri":
                    LogicalUri = (Uri)elementValue;
                    break;

                case @"MajorVersion":
                    MajorVersion = (int)elementValue;
                    break;

                case @"MinorVersion":
                    MinorVersion = (double)elementValue;
                    break;

                case @"MSDomVersion":
                    MSDomVersion = (Version)elementValue;
                    break;

                case @"Platform":
                    Platform = (string)elementValue;
                    break;

                case @"RequestType":
                    RequestType = (string)elementValue;
                    break;

                case @"Source":
                    Source = (string)elementValue;
                    break;

                case @"SourceServer":
                    SourceServer = (string)elementValue;
                    break;

                case @"StatusCode":
                    StatusCode = (int)elementValue;
                    break;

                case @"SubDirectory":
                    SubDirectory = (string)elementValue;
                    break;

                case @"Tables":
                    Tables = (bool)elementValue;
                    break;

                case @"Type":
                    Type = (string)elementValue;
                    break;

                case @"UriHash":
                    break;

                case @"UriQuery":
                    UriQuery = (Uri)elementValue;
                    break;

                case @"UriStem":
                    UriStem = (Uri)elementValue;
                    break;

                case @"UrlReferrer":
                    UrlReferrer = (Uri)elementValue;
                    break;

                case @"UrlReferrerDomain":
                    UrlReferrerDomain = (string)elementValue;
                    break;

                case @"UserAgent":
                    UserAgent = (string)elementValue;
                    break;

                case @"UserHostAddress":
                    UserHostAddress = (IPAddress)elementValue;
                    break;

                case @"VBScript":
                    VBScript = (bool)elementValue;
                    break;

                case @"Version":
                    Version = (string)elementValue;
                    break;

                case @"VirtualRoot":
                    VirtualRoot = (string)elementValue;
                    break;

                case @"W3CDomVersion":
                    W3CDomVersion = (Version)elementValue;
                    break;

                case @"Win16":
                    Win16 = (bool)elementValue;
                    break;

                case @"Win32":
                    Win32 = (bool)elementValue;
                    break;

                default:
                    break;
            }

            return rc;
        }
    }
}
