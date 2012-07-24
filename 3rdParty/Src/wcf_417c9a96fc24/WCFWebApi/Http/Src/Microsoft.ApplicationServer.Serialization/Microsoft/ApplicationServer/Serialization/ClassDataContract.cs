//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
namespace Microsoft.ApplicationServer.Serialization
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Threading;
    using System.Xml;
    using Microsoft.ApplicationServer.Common;
    using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, DataContract>;

    internal sealed class ClassDataContract : DataContract
    {
        [Fx.Tag.SecurityNote(Miscellaneous =
            "RequiresReview - XmlDictionaryString(s) representing the XML namespaces for class members."
            + "statically cached and used from IL generated code. should ideally be Critical."
            + "marked SecurityNode to be callable from transparent IL generated code."
            + "not changed to property to avoid regressing performance; any changes to initalization should be reviewed.")]
        public XmlDictionaryString[] ContractNamespaces;

        [Fx.Tag.SecurityNote(Miscellaneous =
            "RequiresReview - XmlDictionaryString(s) representing the XML element names for class members."
            + "statically cached and used from IL generated code. should ideally be Critical."
            + "marked SecurityNode to be callable from transparent IL generated code."
            + "not changed to property to avoid regressing performance; any changes to initalization should be reviewed.")]
        public XmlDictionaryString[] MemberNames;

        [Fx.Tag.SecurityNote(Miscellaneous =
            "RequiresReview - XmlDictionaryString(s) representing the XML namespaces for class members."
            + "statically cached and used from IL generated code. should ideally be Critical."
            + "marked SecurityNode to be callable from transparent IL generated code."
            + "not changed to property to avoid regressing performance; any changes to initalization should be reviewed.")]
        public XmlDictionaryString[] MemberNamespaces;

        [Fx.Tag.SecurityNote(Critical = "XmlDictionaryString representing the XML namespaces for members of class."
            + "Statically cached and used from IL generated code.")]
        [SecurityCritical]
        XmlDictionaryString[] childElementNamespaces;

        [Fx.Tag.SecurityNote(Critical = "Holds instance of CriticalHelper which keeps state that is cached statically for serialization. "
            + "Static fields are marked SecurityCritical or readonly to prevent data from being modified or leaked to other components in appdomain.")]
        [SecurityCritical]
        ClassDataContractCriticalHelper helper;

        [Fx.Tag.SecurityNote(Critical = "Initializes SecurityCritical field 'helper'",
            Safe = "Doesn't leak anything.")]
        [SecuritySafeCritical]
        internal ClassDataContract()
            : base(new ClassDataContractCriticalHelper())
        {
            InitClassDataContract();
        }

        [Fx.Tag.SecurityNote(Critical = "Initializes SecurityCritical field 'helper'",
            Safe = "Doesn't leak anything.")]
        [SecuritySafeCritical]
        internal ClassDataContract(Type type)
            : base(new ClassDataContractCriticalHelper(type))
        {
            InitClassDataContract();
        }

        [Fx.Tag.SecurityNote(Critical = "Initializes SecurityCritical field 'helper'",
            Safe = "Doesn't leak anything.")]
        [SecuritySafeCritical]
        ClassDataContract(Type type, XmlDictionaryString ns, string[] memberNames)
            : base(new ClassDataContractCriticalHelper(type, ns, memberNames))
        {
            InitClassDataContract();
        }

        [Fx.Tag.SecurityNote(Critical = "Initializes SecurityCritical fields; called from all constructors.")]
        [SecurityCritical]
        void InitClassDataContract()
        {
            this.helper = base.Helper as ClassDataContractCriticalHelper;
            this.ContractNamespaces = helper.ContractNamespaces;
            this.MemberNames = helper.MemberNames;
            this.MemberNamespaces = helper.MemberNamespaces;
        }

        internal ClassDataContract BaseContract
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical baseContract property.",
                Safe = "baseContract only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.BaseContract; }

            [Fx.Tag.SecurityNote(Critical = "Sets the critical baseContract property.")]
            [SecurityCritical]
            set { helper.BaseContract = value; }
        }

        internal List<DataMember> Members
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical members property.",
                Safe = "members only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.Members; }

            [Fx.Tag.SecurityNote(Critical = "Sets the critical members property.",
                Safe = "Protected for write if contract has underlyingType.")]
            [SecurityCritical]
            set { helper.Members = value; }
        }

        public XmlDictionaryString[] ChildElementNamespaces
        {
            [Fx.Tag.SecurityNote(Critical = "Sets the critical childElementNamespaces property.",
                Safe = "childElementNamespaces only needs to be protected for write; initialized in getter if null.")]
            [SecuritySafeCritical]
            get
            {
                if (this.childElementNamespaces == null)
                {
                    lock (this)
                    {
                        if (this.childElementNamespaces == null)
                        {
                            if (helper.ChildElementNamespaces == null)
                            {
                                XmlDictionaryString[] tempChildElementamespaces = CreateChildElementNamespaces();
                                Thread.MemoryBarrier();
                                helper.ChildElementNamespaces = tempChildElementamespaces;
                            }
                            this.childElementNamespaces = helper.ChildElementNamespaces;
                        }
                    }
                }
                return this.childElementNamespaces;
            }
        }

        internal override DataContractDictionary KnownDataContracts
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical knownDataContracts property.",
                Safe = "knownDataContracts only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.KnownDataContracts; }

            [Fx.Tag.SecurityNote(Critical = "Sets the critical knownDataContracts property.",
                Safe = "Protected for write if contract has underlyingType.")]
            [SecurityCritical]
            set { helper.KnownDataContracts = value; }
        }

        internal override bool IsISerializable
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical isISerializable property.",
                Safe = "isISerializable only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.IsISerializable; }

            [Fx.Tag.SecurityNote(Critical = "Sets the critical isISerializable property.",
                Safe = "Protected for write if contract has underlyingType.")]
            [SecurityCritical]
            set { helper.IsISerializable = value; }
        }

        internal bool IsNonAttributedType
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical IsNonAttributedType property.",
                Safe = "IsNonAttributedType only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.IsNonAttributedType; }
        }

        internal bool HasDataContract
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical hasDataContract property.",
                Safe = "hasDataContract only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.HasDataContract; }
        }

        internal bool HasExtensionData
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical hasExtensionData property.",
                Safe = "hasExtensionData only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.HasExtensionData; }
        }

        internal string SerializationExceptionMessage
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical serializationExceptionMessage property.",
                Safe = "serializationExceptionMessage only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.SerializationExceptionMessage; }
        }

        internal string DeserializationExceptionMessage
        {
            [Fx.Tag.SecurityNote(Critical = "Fetches the critical deserializationExceptionMessage property.",
                Safe = "deserializationExceptionMessage only needs to be protected for write.")]
            [SecuritySafeCritical]
            get { return helper.DeserializationExceptionMessage; }
        }

        internal bool IsReadOnlyContract
        {
            get { return this.DeserializationExceptionMessage != null; }
        }

        internal static ClassDataContract CreateClassDataContractForKeyValue(Type type, XmlDictionaryString ns, string[] memberNames)
        {
            return new ClassDataContract(type, ns, memberNames);
        }

        internal static void CheckAndAddMember(List<DataMember> members, DataMember memberContract, Dictionary<string, DataMember> memberNamesTable)
        {
            DataMember existingMemberContract;
            if (memberNamesTable.TryGetValue(memberContract.Name, out existingMemberContract))
            {
                Type declaringType = memberContract.MemberInfo.DeclaringType;
                DataContract.ThrowInvalidDataContractException(
                    declaringType.IsEnum ? 
                    SR.DupEnumMemberValue( 
                        existingMemberContract.MemberInfo.Name,
                        memberContract.MemberInfo.Name,
                        DataContract.GetClrTypeFullName(declaringType),
                        memberContract.Name) :
                    SR.DupMemberName(
                        existingMemberContract.MemberInfo.Name,
                        memberContract.MemberInfo.Name,
                        DataContract.GetClrTypeFullName(declaringType),
                        memberContract.Name),
                    declaringType);
            }
            memberNamesTable.Add(memberContract.Name, memberContract);
            members.Add(memberContract);
        }

        internal static XmlDictionaryString GetChildNamespaceToDeclare(DataContract dataContract, Type childType, XmlDictionary dictionary)
        {
            childType = DataContract.UnwrapNullableType(childType);
            if (!childType.IsEnum && !Globals.TypeOfIXmlSerializable.IsAssignableFrom(childType)
                && DataContract.GetBuiltInDataContract(childType) == null && childType != Globals.TypeOfDBNull)
            {
                string ns = DataContract.GetStableName(childType).Namespace;
                if (ns.Length > 0 && ns != dataContract.Namespace.Value)
                    return dictionary.Add(ns);
            }
            return null;
        }

        [Fx.Tag.SecurityNote(Miscellaneous = "RequiresReview - callers may need to depend on isNonAttributedType for a security decision."
            + "isNonAttributedType must be calculated correctly."
            + "IsNonAttributedTypeValidForSerialization is used as part of the isNonAttributedType calculation and is therefore marked with SecurityNote.",
            Safe = "Does not let caller influence isNonAttributedType calculation; no harm in leaking value.")]
        // check whether a corresponding update is required in DataContractCriticalHelper.CreateDataContract
        static internal bool IsNonAttributedTypeValidForSerialization(Type type)
        {
            if (type.IsArray)
                return false;

            if (type.IsEnum)
                return false;

            if (type.IsGenericParameter)
                return false;

            if (Globals.TypeOfIXmlSerializable.IsAssignableFrom(type))
                return false;

            if (type.IsPointer)
                return false;

            if (type.IsDefined(Globals.TypeOfCollectionDataContractAttribute, false))
                return false;

            Type[] interfaceTypes = type.GetInterfaces();
            foreach (Type interfaceType in interfaceTypes)
            {
                if (CollectionDataContract.IsCollectionInterface(interfaceType))
                    return false;
            }

            if (type.IsSerializable)
                return false;

            if (Globals.TypeOfISerializable.IsAssignableFrom(type))
                return false;

            if (type.IsDefined(Globals.TypeOfDataContractAttribute, false))
                return false;

            if (type == Globals.TypeOfExtensionDataObject)
                return false;

            if (type.IsValueType)
            {
                return type.IsVisible;
            }
            else
            {
                return (type.IsVisible &&
                    type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Globals.EmptyTypeArray, null) != null);
            }
        }

        XmlDictionaryString[] CreateChildElementNamespaces()
        {
            if (Members == null)
                return null;

            XmlDictionaryString[] baseChildElementNamespaces = null;
            if (this.BaseContract != null)
                baseChildElementNamespaces = this.BaseContract.ChildElementNamespaces;
            int baseChildElementNamespaceCount = (baseChildElementNamespaces != null) ? baseChildElementNamespaces.Length : 0;
            XmlDictionaryString[] childElementNamespaces = new XmlDictionaryString[Members.Count + baseChildElementNamespaceCount];
            if (baseChildElementNamespaceCount > 0)
                Array.Copy(baseChildElementNamespaces, 0, childElementNamespaces, 0, baseChildElementNamespaces.Length);

            XmlDictionary dictionary = new XmlDictionary();
            for (int i = 0; i < this.Members.Count; i++)
            {
                childElementNamespaces[i + baseChildElementNamespaceCount] = GetChildNamespaceToDeclare(this, this.Members[i].MemberType, dictionary);
            }

            return childElementNamespaces;
        }

        [Fx.Tag.SecurityNote(Critical = "Calls critical method on helper.",
            Safe = "Doesn't leak anything.")]
        [SecuritySafeCritical]
        void EnsureMethodsImported()
        {
            helper.EnsureMethodsImported();
        }

        [Fx.Tag.SecurityNote(Critical = "Holds all state used for (de)serializing classes."
            + " Since the data is cached statically, we lock down access to it.")]
        [SecurityCritical]
        class ClassDataContractCriticalHelper : DataContract.DataContractCriticalHelper
        {
            ClassDataContract baseContract;
            List<DataMember> members;
            MethodInfo onSerializing, onSerialized;
            MethodInfo onDeserializing, onDeserialized;
            MethodInfo extensionDataSetMethod;
            DataContractDictionary knownDataContracts;
            string serializationExceptionMessage;
            bool isISerializable;
            bool isKnownTypeAttributeChecked;
            bool isMethodChecked;
            bool hasExtensionData;

            [Fx.Tag.SecurityNote(Miscellaneous = "in serialization/deserialization we base the decision whether to Demand SerializationFormatter permission on this value and hasDataContract.")]
            bool isNonAttributedType;

            [Fx.Tag.SecurityNote(Miscellaneous = "in serialization/deserialization we base the decision whether to Demand SerializationFormatter permission on this value and isNonAttributedType.")]
            bool hasDataContract;

            XmlDictionaryString[] childElementNamespaces;

            public XmlDictionaryString[] ContractNamespaces;
            public XmlDictionaryString[] MemberNames;
            public XmlDictionaryString[] MemberNamespaces;

            internal ClassDataContractCriticalHelper()
                : base()
            {
            }

            internal ClassDataContractCriticalHelper(Type type)
                : base(type)
            {
                XmlQualifiedName stableName = GetStableNameAndSetHasDataContract(type);
                if (type == Globals.TypeOfDBNull)
                {
                    this.StableName = stableName;
                    this.members = new List<DataMember>();
                    XmlDictionary dictionary = new XmlDictionary(2);
                    this.Name = dictionary.Add(StableName.Name);
                    this.Namespace = dictionary.Add(StableName.Namespace);
                    this.ContractNamespaces = this.MemberNames = this.MemberNamespaces = new XmlDictionaryString[] { };
                    EnsureMethodsImported();
                    return;
                }
                Type baseType = type.BaseType;
                this.isISerializable = (Globals.TypeOfISerializable.IsAssignableFrom(type));
                SetIsNonAttributedType(type);
                if (this.isISerializable)
                {
                    if (HasDataContract)
                        throw Fx.Exception.AsError(new InvalidDataContractException(SR.ISerializableCannotHaveDataContract(DataContract.GetClrTypeFullName(type))));
                    if (baseType != null && !(baseType.IsSerializable && Globals.TypeOfISerializable.IsAssignableFrom(baseType)))
                        baseType = null;
                }
                this.IsValueType = type.IsValueType;
                if (baseType != null && baseType != Globals.TypeOfObject && baseType != Globals.TypeOfValueType && baseType != Globals.TypeOfUri)
                {
                    DataContract baseContract = DataContract.GetDataContract(baseType);
                    if (baseContract is CollectionDataContract)
                        this.BaseContract = ((CollectionDataContract)baseContract).SharedTypeContract as ClassDataContract;
                    else
                        this.BaseContract = baseContract as ClassDataContract;
                    if (this.BaseContract != null && this.BaseContract.IsNonAttributedType && !this.isNonAttributedType)
                    {
                        throw Fx.Exception.AsError
                            (new InvalidDataContractException(SR.AttributedTypesCannotInheritFromNonAttributedSerializableTypes(
                            DataContract.GetClrTypeFullName(type), DataContract.GetClrTypeFullName(baseType))));
                    }
                }
                else
                    this.BaseContract = null;
                hasExtensionData = (Globals.TypeOfIExtensibleDataObject.IsAssignableFrom(type));
                if (hasExtensionData && !HasDataContract && !IsNonAttributedType)
                    throw Fx.Exception.AsError(new InvalidDataContractException(SR.OnlyDataContractTypesCanHaveExtensionData(DataContract.GetClrTypeFullName(type))));
                if (this.isISerializable)
                    SetDataContractName(stableName);
                else
                {
                    this.StableName = stableName;
                    ImportDataMembers();
                    XmlDictionary dictionary = new XmlDictionary(2 + Members.Count);
                    Name = dictionary.Add(StableName.Name);
                    Namespace = dictionary.Add(StableName.Namespace);

                    int baseMemberCount = 0;
                    int baseContractCount = 0;
                    if (BaseContract == null)
                    {
                        MemberNames = new XmlDictionaryString[Members.Count];
                        MemberNamespaces = new XmlDictionaryString[Members.Count];
                        ContractNamespaces = new XmlDictionaryString[1];
                    }
                    else
                    {
                        if (BaseContract.IsReadOnlyContract)
                        {
                            this.serializationExceptionMessage = BaseContract.SerializationExceptionMessage;
                        }
                        baseMemberCount = BaseContract.MemberNames.Length;
                        MemberNames = new XmlDictionaryString[Members.Count + baseMemberCount];
                        Array.Copy(BaseContract.MemberNames, MemberNames, baseMemberCount);
                        MemberNamespaces = new XmlDictionaryString[Members.Count + baseMemberCount];
                        Array.Copy(BaseContract.MemberNamespaces, MemberNamespaces, baseMemberCount);
                        baseContractCount = BaseContract.ContractNamespaces.Length;
                        ContractNamespaces = new XmlDictionaryString[1 + baseContractCount];
                        Array.Copy(BaseContract.ContractNamespaces, ContractNamespaces, baseContractCount);
                    }
                    ContractNamespaces[baseContractCount] = Namespace;
                    for (int i = 0; i < Members.Count; i++)
                    {
                        MemberNames[i + baseMemberCount] = dictionary.Add(Members[i].Name);
                        MemberNamespaces[i + baseMemberCount] = Namespace;
                    }
                }
                EnsureMethodsImported();
            }

            internal ClassDataContractCriticalHelper(Type type, XmlDictionaryString ns, string[] memberNames)
                : base(type)
            {
                this.StableName = new XmlQualifiedName(GetStableNameAndSetHasDataContract(type).Name, ns.Value);
                ImportDataMembers();
                XmlDictionary dictionary = new XmlDictionary(1 + Members.Count);
                Name = dictionary.Add(StableName.Name);
                Namespace = ns;
                ContractNamespaces = new XmlDictionaryString[] { Namespace };
                MemberNames = new XmlDictionaryString[Members.Count];
                MemberNamespaces = new XmlDictionaryString[Members.Count];
                for (int i = 0; i < Members.Count; i++)
                {
                    Members[i].Name = memberNames[i];
                    MemberNames[i] = dictionary.Add(Members[i].Name);
                    MemberNamespaces[i] = Namespace;
                }
                EnsureMethodsImported();
            }

            void EnsureIsReferenceImported(Type type)
            {
                DataContractAttribute dataContractAttribute;
                bool isReference = false;
                bool hasDataContractAttribute = TryGetDCAttribute(type, out dataContractAttribute);

                if (BaseContract != null)
                {
                    // ALTERED_FOR_PORT: Don't have access to .IsSetExplicilty property
                    if (!this.BaseContract.IsReference && dataContractAttribute.IsReference)
                    {                     
                        DataContract.ThrowInvalidDataContractException(
                                SR.InconsistentIsReference(
                                    DataContract.GetClrTypeFullName(type),
                                    dataContractAttribute.IsReference,
                                    DataContract.GetClrTypeFullName(this.BaseContract.UnderlyingType),
                                    this.BaseContract.IsReference),
                                type);
                    }
                    else
                    {
                        isReference = this.BaseContract.IsReference;
                    }
                }
                else if (hasDataContractAttribute)
                {
                    if (dataContractAttribute.IsReference)
                        isReference = dataContractAttribute.IsReference;
                }

                if (isReference && type.IsValueType)
                {
                    DataContract.ThrowInvalidDataContractException(
                            SR.ValueTypeCannotHaveIsReference(
                                DataContract.GetClrTypeFullName(type),
                                true,
                                false),
                            type);
                    return;
                }

                this.IsReference = isReference;
            }

            void ImportDataMembers()
            {
                Type type = this.UnderlyingType;
                EnsureIsReferenceImported(type);
                List<DataMember> tempMembers = new List<DataMember>();
                Dictionary<string, DataMember> memberNamesTable = new Dictionary<string, DataMember>();

                MemberInfo[] memberInfos;
                if (this.isNonAttributedType)
                {
                    memberInfos = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                }
                else
                {
                    memberInfos = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                for (int i = 0; i < memberInfos.Length; i++)
                {
                    MemberInfo member = memberInfos[i];
                    if (HasDataContract)
                    {
                        object[] memberAttributes = member.GetCustomAttributes(typeof(DataMemberAttribute), false);
                        if (memberAttributes != null && memberAttributes.Length > 0)
                        {
                            if (memberAttributes.Length > 1)
                                ThrowInvalidDataContractException(SR.TooManyDataMembers(DataContract.GetClrTypeFullName(member.DeclaringType), member.Name));

                            DataMember memberContract = new DataMember(member);

                            if (member.MemberType == MemberTypes.Property)
                            {
                                PropertyInfo property = (PropertyInfo)member;

                                MethodInfo getMethod = property.GetGetMethod(true);
                                if (getMethod != null && IsMethodOverriding(getMethod))
                                    continue;
                                MethodInfo setMethod = property.GetSetMethod(true);
                                if (setMethod != null && IsMethodOverriding(setMethod))
                                    continue;
                                if (getMethod == null)
                                    ThrowInvalidDataContractException(SR.NoGetMethodForProperty(property.DeclaringType, property.Name));
                                if (setMethod == null)
                                {
                                    if (!SetIfGetOnlyCollection(memberContract))
                                    {
                                        this.serializationExceptionMessage = SR.NoSetMethodForProperty(property.DeclaringType, property.Name);
                                    }
                                }
                                if (getMethod.GetParameters().Length > 0)
                                    ThrowInvalidDataContractException(SR.IndexedPropertyCannotBeSerialized(property.DeclaringType, property.Name));
                            }
                            else if (member.MemberType != MemberTypes.Field)
                                ThrowInvalidDataContractException(SR.InvalidMember(DataContract.GetClrTypeFullName(type), member.Name));

                            DataMemberAttribute memberAttribute = (DataMemberAttribute)memberAttributes[0];
                            // ALTERED_FOR_PORT: Don't have access to .IsSetExplicilty property
                            if (memberAttribute.Name != null && memberAttribute.Name.Length > 0)
                                memberContract.Name = memberAttribute.Name;
                            else
                                memberContract.Name = member.Name;

                            memberContract.Name = DataContract.EncodeLocalName(memberContract.Name);
                            memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                            memberContract.IsRequired = memberAttribute.IsRequired;
                            if (memberAttribute.IsRequired && this.IsReference)
                            {
                                ThrowInvalidDataContractException(
                                    SR.IsRequiredDataMemberOnIsReferenceDataContractType(
                                    DataContract.GetClrTypeFullName(member.DeclaringType),
                                    member.Name, true), type);
                            }
                            memberContract.EmitDefaultValue = memberAttribute.EmitDefaultValue;
                            memberContract.Order = memberAttribute.Order;
                            CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                        }
                    }
                    else if (this.isNonAttributedType)
                    {
                        FieldInfo field = member as FieldInfo;
                        PropertyInfo property = member as PropertyInfo;
                        if ((field == null && property == null) || (field != null && field.IsInitOnly))
                            continue;

                        object[] memberAttributes = member.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), false);
                        if (memberAttributes != null && memberAttributes.Length > 0)
                        {
                            if (memberAttributes.Length > 1)
                                ThrowInvalidDataContractException(SR.TooManyIgnoreDataMemberAttributes(DataContract.GetClrTypeFullName(member.DeclaringType), member.Name));
                            else
                                continue;
                        }
                        DataMember memberContract = new DataMember(member);
                        if (property != null)
                        {
                            MethodInfo getMethod = property.GetGetMethod();
                            if (getMethod == null || IsMethodOverriding(getMethod) || getMethod.GetParameters().Length > 0)
                                continue;

                            MethodInfo setMethod = property.GetSetMethod(true);
                            if (setMethod == null)
                            {
                                if (!SetIfGetOnlyCollection(memberContract))
                                    continue;
                            }
                            else
                            {
                                if (!setMethod.IsPublic || IsMethodOverriding(setMethod))
                                    continue;
                            }

                            //skip ExtensionData member of type ExtensionDataObject if IExtensibleDataObject is implemented in non-attributed type
                            if (this.hasExtensionData && memberContract.MemberType == Globals.TypeOfExtensionDataObject
                                && member.Name == Globals.ExtensionDataObjectPropertyName)
                                continue;
                        }

                        memberContract.Name = DataContract.EncodeLocalName(member.Name);
                        memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                        CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                    }
                    else
                    {
                        FieldInfo field = member as FieldInfo;
                        if (field != null && !field.IsNotSerialized)
                        {
                            DataMember memberContract = new DataMember(member);

                            memberContract.Name = DataContract.EncodeLocalName(member.Name);
                            object[] optionalFields = field.GetCustomAttributes(Globals.TypeOfOptionalFieldAttribute, false);
                            if (optionalFields == null || optionalFields.Length == 0)
                            {
                                if (this.IsReference)
                                {
                                    ThrowInvalidDataContractException(
                                        SR.NonOptionalFieldMemberOnIsReferenceSerializableType(
                                        DataContract.GetClrTypeFullName(member.DeclaringType),
                                        member.Name, true), type);
                                }
                                memberContract.IsRequired = true;
                            }
                            memberContract.IsNullable = DataContract.IsTypeNullable(memberContract.MemberType);
                            CheckAndAddMember(tempMembers, memberContract, memberNamesTable);
                        }
                    }
                }
                if (tempMembers.Count > 1)
                    tempMembers.Sort(DataMemberComparer.Singleton);

                SetIfMembersHaveConflict(tempMembers);

                Thread.MemoryBarrier();
                members = tempMembers;
            }

            bool SetIfGetOnlyCollection(DataMember memberContract)
            {
                //OK to call IsCollection here since the use of surrogated collection types is not supported in get-only scenarios
                if (CollectionDataContract.IsCollection(memberContract.MemberType, false /*isConstructorRequired*/) && !memberContract.MemberType.IsValueType)
                {
                    memberContract.IsGetOnlyCollection = true;
                    return true;
                }
                return false;
            }

            void SetIfMembersHaveConflict(List<DataMember> members)
            {
                if (BaseContract == null)
                    return;

                int baseTypeIndex = 0;
                List<Member> membersInHierarchy = new List<Member>();
                foreach (DataMember member in members)
                {
                    membersInHierarchy.Add(new Member(member, this.StableName.Namespace, baseTypeIndex));
                }
                ClassDataContract currContract = BaseContract;
                while (currContract != null)
                {
                    baseTypeIndex++;
                    foreach (DataMember member in currContract.Members)
                    {
                        membersInHierarchy.Add(new Member(member, currContract.StableName.Namespace, baseTypeIndex));
                    }
                    currContract = currContract.BaseContract;
                }

                IComparer<Member> comparer = DataMemberConflictComparer.Singleton;
                membersInHierarchy.Sort(comparer);

                for (int i = 0; i < membersInHierarchy.Count - 1; i++)
                {
                    int startIndex = i;
                    int endIndex = i;
                    bool hasConflictingType = false;
                    while (endIndex < membersInHierarchy.Count - 1
                        && String.CompareOrdinal(membersInHierarchy[endIndex].member.Name, membersInHierarchy[endIndex + 1].member.Name) == 0
                        && String.CompareOrdinal(membersInHierarchy[endIndex].ns, membersInHierarchy[endIndex + 1].ns) == 0)
                    {
                        membersInHierarchy[endIndex].member.ConflictingMember = membersInHierarchy[endIndex + 1].member;
                        if (!hasConflictingType)
                        {
                            if (membersInHierarchy[endIndex + 1].member.HasConflictingNameAndType)
                            {
                                hasConflictingType = true;
                            }
                            else
                            {
                                hasConflictingType = (membersInHierarchy[endIndex].member.MemberType != membersInHierarchy[endIndex + 1].member.MemberType);
                            }
                        }
                        endIndex++;
                    }

                    if (hasConflictingType)
                    {
                        for (int j = startIndex; j <= endIndex; j++)
                        {
                            membersInHierarchy[j].member.HasConflictingNameAndType = true;
                        }
                    }

                    i = endIndex + 1;
                }
            }

            [Fx.Tag.SecurityNote(Critical = "Sets the critical hasDataContract field.",
                Safe = "Uses a trusted critical API (DataContract.GetStableName) to calculate the value, does not accept the value from the caller.")]
            [SecuritySafeCritical]
            XmlQualifiedName GetStableNameAndSetHasDataContract(Type type)
            {
                return DataContract.GetStableName(type, out this.hasDataContract);
            }

            [Fx.Tag.SecurityNote(Miscellaneous = "RequiresReview - callers may need to depend on isNonAttributedType for a security decision."
                + "isNonAttributedType must be calculated correctly."
                + "SetIsNonAttributedType should not be called before GetStableNameAndSetHasDataContract since it is dependent on the correct calculation of hasDataContract.",
                Safe = "Does not let caller influence isNonAttributedType calculation; no harm in leaking value.")]
            void SetIsNonAttributedType(Type type)
            {
                this.isNonAttributedType = !type.IsSerializable && !this.hasDataContract && IsNonAttributedTypeValidForSerialization(type);
            }

            static bool IsMethodOverriding(MethodInfo method)
            {
                return method.IsVirtual && ((method.Attributes & MethodAttributes.NewSlot) == 0);
            }

            internal void EnsureMethodsImported()
            {
                if (!isMethodChecked && UnderlyingType != null)
                {
                    lock (this)
                    {
                        if (!isMethodChecked)
                        {
                            Type type = this.UnderlyingType;
                            MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            for (int i = 0; i < methods.Length; i++)
                            {
                                MethodInfo method = methods[i];
                                Type prevAttributeType = null;
                                ParameterInfo[] parameters = method.GetParameters();
                                if (HasExtensionData && IsValidExtensionDataSetMethod(method, parameters))
                                {
                                    if (method.Name == Globals.ExtensionDataSetExplicitMethod || !method.IsPublic)
                                        extensionDataSetMethod = XmlFormatGeneratorStatics.ExtensionDataSetExplicitMethodInfo;
                                    else
                                        extensionDataSetMethod = method;
                                }
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnSerializingAttribute, onSerializing, ref prevAttributeType))
                                    onSerializing = method;
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnSerializedAttribute, onSerialized, ref prevAttributeType))
                                    onSerialized = method;
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnDeserializingAttribute, onDeserializing, ref prevAttributeType))
                                    onDeserializing = method;
                                if (IsValidCallback(method, parameters, Globals.TypeOfOnDeserializedAttribute, onDeserialized, ref prevAttributeType))
                                    onDeserialized = method;
                            }
                            Thread.MemoryBarrier();
                            isMethodChecked = true;
                        }
                    }
                }
            }

            bool IsValidExtensionDataSetMethod(MethodInfo method, ParameterInfo[] parameters)
            {
                if (method.Name == Globals.ExtensionDataSetExplicitMethod || method.Name == Globals.ExtensionDataSetMethod)
                {
                    if (extensionDataSetMethod != null)
                        ThrowInvalidDataContractException(SR.DuplicateExtensionDataSetMethod(method, extensionDataSetMethod, DataContract.GetClrTypeFullName(method.DeclaringType)));
                    if (method.ReturnType != Globals.TypeOfVoid)
                        DataContract.ThrowInvalidDataContractException(SR.ExtensionDataSetMustReturnVoid(DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
                    if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfExtensionDataObject)
                        DataContract.ThrowInvalidDataContractException(SR.ExtensionDataSetParameterInvalid(DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfExtensionDataObject), method.DeclaringType);
                    return true;
                }
                return false;
            }

            static bool IsValidCallback(MethodInfo method, ParameterInfo[] parameters, Type attributeType, MethodInfo currentCallback, ref Type prevAttributeType)
            {
                if (method.IsDefined(attributeType, false))
                {
                    if (currentCallback != null)
                        DataContract.ThrowInvalidDataContractException(SR.DuplicateCallback(method, currentCallback, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
                    else if (prevAttributeType != null)
                        DataContract.ThrowInvalidDataContractException(SR.DuplicateAttribute(prevAttributeType, attributeType, DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
                    else if (method.IsVirtual)
                        DataContract.ThrowInvalidDataContractException(SR.CallbacksCannotBeVirtualMethods(method, DataContract.GetClrTypeFullName(method.DeclaringType), attributeType), method.DeclaringType);
                    else
                    {
                        if (method.ReturnType != Globals.TypeOfVoid)
                            DataContract.ThrowInvalidDataContractException(SR.CallbackMustReturnVoid(DataContract.GetClrTypeFullName(method.DeclaringType), method), method.DeclaringType);
                        if (parameters == null || parameters.Length != 1 || parameters[0].ParameterType != Globals.TypeOfStreamingContext)
                            DataContract.ThrowInvalidDataContractException(SR.CallbackParameterInvalid(DataContract.GetClrTypeFullName(method.DeclaringType), method, Globals.TypeOfStreamingContext), method.DeclaringType);

                        prevAttributeType = attributeType;
                    }
                    return true;
                }
                return false;
            }

            internal ClassDataContract BaseContract
            {
                get { return baseContract; }
                set
                {
                    baseContract = value;
                    if (baseContract != null && IsValueType)
                        ThrowInvalidDataContractException(SR.ValueTypeCannotHaveBaseType(StableName.Name, StableName.Namespace, baseContract.StableName.Name, baseContract.StableName.Namespace));
                }
            }

            internal List<DataMember> Members
            {
                get { return members; }
                set { members = value; }
            }

            internal MethodInfo OnSerializing
            {
                get
                {
                    EnsureMethodsImported();
                    return onSerializing;
                }
            }

            internal MethodInfo OnSerialized
            {
                get
                {
                    EnsureMethodsImported();
                    return onSerialized;
                }
            }

            internal MethodInfo OnDeserializing
            {
                get
                {
                    EnsureMethodsImported();
                    return onDeserializing;
                }
            }

            internal MethodInfo OnDeserialized
            {
                get
                {
                    EnsureMethodsImported();
                    return onDeserialized;
                }
            }

            internal MethodInfo ExtensionDataSetMethod
            {
                get
                {
                    EnsureMethodsImported();
                    return extensionDataSetMethod;
                }
            }

            internal override DataContractDictionary KnownDataContracts
            {
                get
                {
                    if (!isKnownTypeAttributeChecked && UnderlyingType != null)
                    {
                        lock (this)
                        {
                            if (!isKnownTypeAttributeChecked)
                            {
                                knownDataContracts = DataContract.ImportKnownTypeAttributes(this.UnderlyingType);
                                Thread.MemoryBarrier();
                                isKnownTypeAttributeChecked = true;
                            }
                        }
                    }
                    return knownDataContracts;
                }
                set { knownDataContracts = value; }
            }

            internal string SerializationExceptionMessage
            {
                get { return serializationExceptionMessage; }
            }

            internal string DeserializationExceptionMessage
            {
                get
                {
                    if (serializationExceptionMessage == null)
                    {
                        return null;
                    }
                    else
                    {
                        return SR.ReadOnlyClassDeserialization(this.serializationExceptionMessage);
                    }
                }
            }

            internal override bool IsISerializable
            {
                get { return isISerializable; }
                set { isISerializable = value; }
            }

            internal bool HasDataContract
            {
                get { return hasDataContract; }
            }

            internal bool HasExtensionData
            {
                get { return hasExtensionData; }
            }

            internal bool IsNonAttributedType
            {
                get { return isNonAttributedType; }
            }

            internal ConstructorInfo GetNonAttributedTypeConstructor()
            {
                if (!this.IsNonAttributedType)
                    return null;

                Type type = UnderlyingType;

                if (type.IsValueType)
                    return null;

                ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Globals.EmptyTypeArray, null);
                if (ctor == null)
                    throw Fx.Exception.AsError(new InvalidDataContractException(SR.NonAttributedSerializableTypesMustHaveDefaultConstructor(DataContract.GetClrTypeFullName(type))));

                return ctor;
            }

            public XmlDictionaryString[] ChildElementNamespaces
            {
                get { return childElementNamespaces; }
                set { childElementNamespaces = value; }
            }

            static Type[] serInfoCtorArgs;
            static Type[] SerInfoCtorArgs
            {
                get
                {
                    if (serInfoCtorArgs == null)
                        serInfoCtorArgs = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };
                    return serInfoCtorArgs;
                }
            }

            internal struct Member
            {
                internal Member(DataMember member, string ns, int baseTypeIndex)
                {
                    this.member = member;
                    this.ns = ns;
                    this.baseTypeIndex = baseTypeIndex;
                }
                internal DataMember member;
                internal string ns;
                internal int baseTypeIndex;
            }

            internal class DataMemberConflictComparer : IComparer<Member>
            {
                public int Compare(Member x, Member y)
                {
                    int nsCompare = String.CompareOrdinal(x.ns, y.ns);
                    if (nsCompare != 0)
                        return nsCompare;

                    int nameCompare = String.CompareOrdinal(x.member.Name, y.member.Name);
                    if (nameCompare != 0)
                        return nameCompare;

                    return x.baseTypeIndex - y.baseTypeIndex;
                }

                internal static DataMemberConflictComparer Singleton = new DataMemberConflictComparer();
            }

        }

        [Fx.Tag.SecurityNote(Critical = "Sets critical properties on ClassDataContract .",
            Safe = "Called during schema import/code generation.")]
        [SecuritySafeCritical]
        internal override DataContract BindGenericParameters(DataContract[] paramContracts, Dictionary<DataContract, DataContract> boundContracts)
        {
            Type type = UnderlyingType;
            if (!type.IsGenericType || !type.ContainsGenericParameters)
                return this;

            lock (this)
            {
                DataContract boundContract;
                if (boundContracts.TryGetValue(this, out boundContract))
                    return boundContract;

                ClassDataContract boundClassContract = new ClassDataContract();
                boundContracts.Add(this, boundClassContract);
                XmlQualifiedName stableName;
                object[] genericParams;
                if (type.IsGenericTypeDefinition)
                {
                    stableName = this.StableName;
                    genericParams = paramContracts;
                }
                else
                {
                    //partial Generic: Construct stable name from its open generic type definition
                    stableName = DataContract.GetStableName(type.GetGenericTypeDefinition());
                    Type[] paramTypes = type.GetGenericArguments();
                    genericParams = new object[paramTypes.Length];
                    for (int i = 0; i < paramTypes.Length; i++)
                    {
                        Type paramType = paramTypes[i];
                        if (paramType.IsGenericParameter)
                            genericParams[i] = paramContracts[paramType.GenericParameterPosition];
                        else
                            genericParams[i] = paramType;
                    }
                }
                boundClassContract.StableName = CreateQualifiedName(DataContract.ExpandGenericParameters(XmlConvert.DecodeName(stableName.Name), new GenericNameProvider(DataContract.GetClrTypeFullName(this.UnderlyingType), genericParams)), stableName.Namespace);
                if (BaseContract != null)
                    boundClassContract.BaseContract = (ClassDataContract)BaseContract.BindGenericParameters(paramContracts, boundContracts);
                boundClassContract.IsISerializable = this.IsISerializable;
                boundClassContract.IsValueType = this.IsValueType;
                boundClassContract.IsReference = this.IsReference;
                if (Members != null)
                {
                    boundClassContract.Members = new List<DataMember>(Members.Count);
                    foreach (DataMember member in Members)
                        boundClassContract.Members.Add(member.BindGenericParameters(paramContracts, boundContracts));
                }
                return boundClassContract;
            }
        }

        internal override bool Equals(object other, Dictionary<DataContractPairKey, object> checkedContracts)
        {
            if (IsEqualOrChecked(other, checkedContracts))
                return true;

            if (base.Equals(other, checkedContracts))
            {
                ClassDataContract dataContract = other as ClassDataContract;
                if (dataContract != null)
                {
                    if (IsISerializable)
                    {
                        if (!dataContract.IsISerializable)
                            return false;
                    }
                    else
                    {
                        if (dataContract.IsISerializable)
                            return false;

                        if (Members == null)
                        {
                            if (dataContract.Members != null)
                            {
                                // check that all the datamembers in dataContract.Members are optional
                                if (!IsEveryDataMemberOptional(dataContract.Members))
                                    return false;
                            }
                        }
                        else if (dataContract.Members == null)
                        {
                            // check that all the datamembers in Members are optional
                            if (!IsEveryDataMemberOptional(Members))
                                return false;
                        }
                        else
                        {
                            Dictionary<string, DataMember> membersDictionary = new Dictionary<string, DataMember>(Members.Count);
                            List<DataMember> dataContractMembersList = new List<DataMember>();
                            for (int i = 0; i < Members.Count; i++)
                            {
                                membersDictionary.Add(Members[i].Name, Members[i]);
                            }

                            for (int i = 0; i < dataContract.Members.Count; i++)
                            {
                                // check that all datamembers common to both datacontracts match
                                DataMember dataMember;
                                if (membersDictionary.TryGetValue(dataContract.Members[i].Name, out dataMember))
                                {
                                    if (dataMember.Equals(dataContract.Members[i], checkedContracts))
                                    {
                                        membersDictionary.Remove(dataMember.Name);
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                // otherwise save the non-matching datamembers for later verification 
                                else
                                {
                                    dataContractMembersList.Add(dataContract.Members[i]);
                                }
                            }

                            // check that datamembers left over from either datacontract are optional
                            if (!IsEveryDataMemberOptional(membersDictionary.Values))
                                return false;
                            if (!IsEveryDataMemberOptional(dataContractMembersList))
                                return false;

                        }
                    }

                    if (BaseContract == null)
                        return (dataContract.BaseContract == null);
                    else if (dataContract.BaseContract == null)
                        return false;
                    else
                        return BaseContract.Equals(dataContract.BaseContract, checkedContracts);
                }
            }
            return false;
        }

        bool IsEveryDataMemberOptional(IEnumerable<DataMember> dataMembers)
        {
            foreach (DataMember dataMember in dataMembers)
            {
                if (dataMember.IsRequired)
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal class DataMemberComparer : IComparer<DataMember>
        {
            public int Compare(DataMember x, DataMember y)
            {
                int orderCompare = x.Order - y.Order;
                if (orderCompare != 0)
                    return orderCompare;

                return String.CompareOrdinal(x.Name, y.Name);
            }

            internal static DataMemberComparer Singleton = new DataMemberComparer();
        }
    }
}

