// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "This is just a code sample and we don't ship this assembly")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "JsonValue", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is part of our API")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "JsonArray", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is part of our API")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "DateTimeOffset", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is part of the framework")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "DateTime", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is part of the framework")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String)", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is just a sample, no need for separate resource files")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String,System.Object,System.Object,System.Object)", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is just a sample, no need for separate resource files")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String,System.Object,System.Object)", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is just a sample, no need for separate resource files")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Console.WriteLine(System.String,System.Object)", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is just a sample, no need for separate resource files")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is expected when using dynamics: for some reason code analysis includes generated code")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Scope = "member", Target = "JsonValueDynamic.Program.#Main()", Justification = "This is expected when using dynamics: for some reason code analysis includes generated code")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode", Scope = "type", Target = "JsonValueDynamic.Program", Justification = "This is expected when using dynamics: for some reason code analysis includes generated code")]
