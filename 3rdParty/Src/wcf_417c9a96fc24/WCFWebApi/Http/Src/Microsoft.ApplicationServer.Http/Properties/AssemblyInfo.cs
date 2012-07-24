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
[assembly: AssemblyTitle("Microsoft.ApplicationServer.Http")]
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

// Friend assemblies - CIT
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.Http.CIT.Unit")]

// Friend assemblies -- CIT Moles
[assembly: InternalsVisibleTo("Microsoft.ApplicationServer.Http.Moles")]
    
[assembly: NeutralResourcesLanguageAttribute("en-US")]
[assembly: SuppressMessage(FxCop.Category.Design, FxCop.Rule.AssembliesShouldHaveValidStrongNames, Justification = "These assemblies are delay-signed.")]
[assembly: SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidNamespacesWithFewTypes, Justification = "Classes are grouped logically for user clarity.", Scope = "Namespace", Target = "Microsoft.ApplicationServer.Http")]
[assembly: SuppressMessage(FxCop.Category.Design, FxCop.Rule.AvoidNamespacesWithFewTypes, Justification = "Classes are grouped logically for user clarity.", Scope = "Namespace", Target = "Microsoft.ApplicationServer.Http.Channels")]
