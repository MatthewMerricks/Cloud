﻿// <copyright file="AssemblyInfo.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Microsoft.ServiceModel.Web.jQuery")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct("Microsoft.ServiceModel.Web.jQuery")]
[assembly: AssemblyCopyright("Copyright © Microsoft Corp. 2010")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("52a8fdaf-835a-498a-8b04-a0ac63cb9558")]

[assembly:CLSCompliant(true)]

[assembly: NeutralResourcesLanguage("en-US")]

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
[assembly: AssemblyVersion("1.0.0.0")]

// AssemblyFileVersion attribute is generated automatically by a custom MSBuild task
// [assembly: AssemblyFileVersion("1.0.0.0")] 

// Disabling unecessary FxCop/StyleCop violation
[assembly:System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709",
    Justification = "The name should not be JQuery, since jQuery has a lowercase 'j'")]