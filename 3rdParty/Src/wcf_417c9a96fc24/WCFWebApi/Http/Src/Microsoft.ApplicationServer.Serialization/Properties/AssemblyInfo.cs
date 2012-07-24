// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.ApplicationServer.Common;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.ApplicationServer.Serialization")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyCopyright("Copyright © Microsoft 2010")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.3.0.0")]

// Friend assemblies - product
// Add entries using the test key under TEST_ASSEMBLY and the normal key otherwise.
// This is required because building the CIT's forces use of the test key for all projects.
#if TEST_ASSEMBLY
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.Http, PublicKey=0024000004800000940000000602000000240000525341310004000001000100197C25D0A04F73CB271E8181DBA1C0C713DF8DEEBB25864541A66670500F34896D280484B45FE1FF6C29F2EE7AA175D8BCBD0C83CC23901A894A86996030F6292CE6EDA6E6F3E6C74B3C5A3DED4903C951E6747E6102969503360F7781BF8BF015058EB89B7621798CCC85AACA036FF1BC1556BB7F62DE15908484886AA8BBAE")]
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.ServiceModel.Tools, PublicKey=0024000004800000940000000602000000240000525341310004000001000100197c25d0a04f73cb271e8181dba1c0c713df8deebb25864541a66670500f34896d280484b45fe1ff6c29f2ee7aa175d8bcbd0c83cc23901a894a86996030f6292ce6eda6e6f3e6c74b3c5a3ded4903c951e6747e6102969503360f7781bf8bf015058eb89b7621798ccc85aaca036ff1bc1556bb7f62de15908484886aa8bbae")]
#else
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.Http")]
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.ServiceModel.Tools")]
#endif

// Friend assemblies - CIT
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.Serialization.CIT.Unit.DataContract")]

[assembly: NeutralResourcesLanguageAttribute("en-US")]
[assembly: SuppressMessage(FxCop.Category.Design, FxCop.Rule.AssembliesShouldHaveValidStrongNames, Justification = "These assemblies are delay-signed.")]
[assembly: SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidNamespacesWithFewTypes, Justification = "Classes are grouped logically for user clarity.", Scope = "Namespace", Target = "Microsoft.ApplicationServer.ServiceModel.Channels")]

