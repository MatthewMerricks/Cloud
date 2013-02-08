//
// ContainsMetadataDictionaryAttribute.cs
// Cloud Windows
//
// Created By DavidBruck.
// Copyright (c) Cloud.com. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudApiPublic.JsonContracts
{
    /// <summary>
    /// Mark any class/struct/interface which contains a <see cref="MetadataDictionary"/> with this attribute and also mark any class/struct/interface with this attribute if it contains something with this attribute;
    /// this is used to remove/add Json pairs with key: "__type" from serialization/deserialization (respectively) without breaking DataContractJsonSerializer within Helpers.ProcessHttp(of T)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
    internal sealed class ContainsMetadataDictionaryAttribute : Attribute { }
}