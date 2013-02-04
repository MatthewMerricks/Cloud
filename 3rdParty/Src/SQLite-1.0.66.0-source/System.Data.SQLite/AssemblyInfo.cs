using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security;
using System.Resources;

#if !PLATFORM_COMPACTFRAMEWORK
using System.Runtime.ConstrainedExecution;
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("System.Data.SQLite")]
[assembly: AssemblyDescription("ADO.NET 2.0 Data Provider for SQLite")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("http://sqlite.phxsoftware.com")]
[assembly: AssemblyProduct("System.Data.SQLite")]
[assembly: AssemblyCopyright("Public Domain")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if PLATFORM_COMPACTFRAMEWORK && RETARGETABLE
[assembly: AssemblyFlags(AssemblyNameFlags.Retargetable)]
#endif

//  Setting ComVisible to false makes the types in this assembly not visible 
//  to COM componenets.  If you need to access a type in this assembly from 
//  COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("System.Data.SQLite.Linq, PublicKey=0024000004800000140100000602000000240000525341310008000001000100311414d8932a197570430a48c993143584131d3cb3dd6e7d8d97f19b06069e20f54d9d2a68d140685ecaac30eecc29c91e2a2dd9e0f3b4cb6eb354fd726cb888e9d8a92a5f42fe6233401039352f9e0933787adf74018c620e9aab181a0adb3a898bc8c7f02e03ba5ca14f8492ee7543d8d6a9f27990a581b54ff07d79386387f14adac2b1fbf81d293bf5c662c60bf8812d07340e17276edd8172bf9aaa89589d7a66c0694d25f77d03f130e002720a2a98e28887633492b9eb927741e62a7a92169d82ef82e7bc5d3b61cca8f3593d8efb9dd206fe5ee4183645eadf8010b58f44924f06177cbda03aace8abe47f49768c5879a4a307a137dc53e45efb11b0")]
[assembly: NeutralResourcesLanguage("en")]

#if !PLATFORM_COMPACTFRAMEWORK
[assembly: AllowPartiallyTrustedCallers]
[assembly: ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
[assembly: System.Security.SecurityRules(System.Security.SecurityRuleSet.Level1)]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
[assembly: AssemblyVersion("1.0.66.0")]
#if !PLATFORM_COMPACTFRAMEWORK
[assembly: AssemblyFileVersion("1.0.66.0")]
#endif
