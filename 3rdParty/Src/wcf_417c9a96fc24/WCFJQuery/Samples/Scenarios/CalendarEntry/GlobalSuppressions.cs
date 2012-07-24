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

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Strong name signing not required for codeplex.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "CalendarEntry.CalendarResource.#Post(System.Json.JsonObject)", Justification = "Service contract implementation.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "CalendarEntry.ContactsResource.#GetAll()", Justification = "Service contract implementation.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Scope = "member", Target = "CalendarEntry.ContactsResource.#GetAll()", Justification = "Service contract implementation.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "jv", Scope = "member", Target = "CalendarEntry.CalendarResource+CustomValidator.#ValidateGuestEmails(System.Json.JsonValue,System.ComponentModel.DataAnnotations.ValidationContext)", Justification = "Parameter is required per contract for Custom Validator methods.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "jv", Scope = "member", Target = "CalendarEntry.CalendarResource+CustomValidator.#ValidateMeetingTime(System.Json.JsonValue,System.ComponentModel.DataAnnotations.ValidationContext)", Justification = "Parameter is required per contract for Custom Validator methods.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "jv", Scope = "member", Target = "CalendarEntry.CalendarResource+CustomValidator.#ValidateReminder(System.Json.JsonValue,System.ComponentModel.DataAnnotations.ValidationContext)", Justification = "Parameter is required per contract for Custom Validator methods.")]
