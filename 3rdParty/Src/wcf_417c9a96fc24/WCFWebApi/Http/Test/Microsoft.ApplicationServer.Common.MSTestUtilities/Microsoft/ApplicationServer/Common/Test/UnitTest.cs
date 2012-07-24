// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.ApplicationServer.Common.Test.Framework
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.Moles.Framework.Moles;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;

    /// <summary>
    /// Base class that does basic unit test class validation.
    /// </summary>
    /// <remarks>
    /// Unit test classes should derive from this class and call the
    /// <see cref="ValidateUnitTestClass"/> to self-validate using MSTest.
    /// </remarks>
    [TestClass]
    public abstract class UnitTest
    {
        private static readonly string testErrorPrefix = "Unit test error: ";
        private static readonly string typeIsCorrectMethodName = "TypeIsCorrect";
        private static readonly string[] genericSuffixes = new string[] { "OfT", "[T]", "<T>", "Generic" };

        private const BindingFlags publicDeclaredInstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        private const BindingFlags publicOrPrivateDeclaredInstanceBindingFlags = publicDeclaredInstanceBindingFlags | BindingFlags.NonPublic;
        private const BindingFlags publicOrPrivateStaticOrInstance = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags publicOrPrivateInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private MethodInfo[] testMethods;
        private Type typeUnderTest;

        protected UnitTestLevel UnitTestLevel
        {
            get
            {
                return UnitTestLevelOfMember(this.GetType());
            }
        }

        public Type TypeUnderTest
        {
            get
            {
                if (this.typeUnderTest == null)
                {
                    this.typeUnderTest = GetTypeUnderTest(this.GetType());
                }

                return this.typeUnderTest;
            }

            set
            {
                this.typeUnderTest = value;
            }
        }

        private MethodInfo[] TestMethods
        {
            get
            {
                if (this.testMethods == null)
                {
                    this.testMethods = GetTestMethods(this.GetType());
                }

                return this.testMethods;
            }
        }

        /// <summary>
        /// Subclasses are required to implement this, and should call <see cref="ValidateUnitTestClass"/>.
        /// </summary>
        public abstract void UnitTestClassIsCorrect();

        public static Type GetTypeUnderTest(Type unitTestClassType)
        {
            UnitTestTypeAttribute unitTestTypeAttribute =
                unitTestClassType.GetCustomAttributes(typeof(UnitTestTypeAttribute), false)
                    .Cast<UnitTestTypeAttribute>()
                        .SingleOrDefault();

            Type type = unitTestTypeAttribute == null ? null : unitTestTypeAttribute.Type;

            if (type == null && typeof(UnitTest).IsAssignableFrom(unitTestClassType))
            {
                if (unitTestClassType.BaseType.IsGenericType)
                {
                    type = unitTestClassType.BaseType.GetGenericArguments()[0];
                }
            }

            return type;
        }

        /// <summary>
        /// Validates the derived class adheres to a common set of conventions.
        /// </summary>
        /// <remarks>
        /// This method discovers and executes all the test methods in this current <see cref="UnitTest"/>
        /// base class.  To add more validation, simply add more test methods to this base class.
        /// Please mark the test methods private so that they cannot be inherited by the derived tests.
        /// </remarks>
        protected void ValidateUnitTestClass()
        {
            UnitTestLevel classLevel = UnitTestLevelOfMember(this.GetType());
            MethodInfo[] baseMethods = typeof(UnitTest).GetMethods(publicOrPrivateDeclaredInstanceBindingFlags)
                    .Where((m) => m.GetCustomAttributes(typeof(TestMethodAttribute), false).Any())
                        .Where((m) => UnitTestLevelOfMember(m) <= classLevel)
                            .ToArray();

            StringBuilder sb = new StringBuilder();

            foreach (MethodInfo methodInfo in baseMethods)
            {
                try
                {
                    methodInfo.Invoke(this, new object[0]);
                }
                catch (TargetInvocationException targetInvocationException)
                {
                    // Strip off portion MSTest adds if we know this is one of our messages.
                    string message = targetInvocationException.InnerException.Message;
                    int indexOfPrefix = message.IndexOf(testErrorPrefix);
                    if (indexOfPrefix >= 0)
                    {
                        message = message.Substring(indexOfPrefix);
                    }

                    // First error gets newline to put all summary errors below MSTest "Assert.Fail" message
                    if (sb.Length == 0)
                    {
                        sb.AppendLine();
                    }

                    sb.AppendLine(message);
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(sb.ToString());
            }
            else
            {
                string untestedMembers;
                switch (this.UnitTestLevel)
                {
                    case UnitTestLevel.None:
                        System.Diagnostics.Debug.WriteLine(
                            string.Format(
                                "'{0}' is not yet enabled for unit test verification.\r\nAdd [UnitTestLevel(UnitTestLevel.NotReady)] to the class to enable basic verification.", 
                                this.GetType().Name));
                        break;

                    case UnitTestLevel.NotReady:

                        untestedMembers = this.GetSummaryOfUntestedMembers();
                        if (!string.IsNullOrWhiteSpace(untestedMembers))
                        {
                            System.Diagnostics.Debug.WriteLine(untestedMembers);
                        }

                        Assert.Inconclusive(string.Format("'{0}' passed basic verification but is marked 'Not Ready'.\r\nAdd [UnitTestLevel(UnitTestLevel.InProgress)] or [UnitTestLevel(UnitTestLevel.Complete)] to the class when it is ready for check in.", this.GetType().Name));

                        break;

                    case UnitTestLevel.InProgress:
                        System.Diagnostics.Debug.WriteLine(string.Format("'{0}'passed basic verification but is marked 'In Progress'.\r\nAdd [UnitTestLevel(UnitTestLevel.Complete)] to the class when it is complete.", this.GetType().Name));
                        untestedMembers = this.GetSummaryOfUntestedMembers();
                        if (!string.IsNullOrWhiteSpace(untestedMembers))
                        {
                            System.Diagnostics.Debug.WriteLine(untestedMembers);
                        }

                        break;

                    case UnitTestLevel.Complete:
                        break;
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must have declared the type it is testing.")]
        [UnitTestLevel(UnitTestLevel.NotReady)]
        private void HasTypeUnderTest()
        {
            Type typeUnderTest = this.TypeUnderTest;
            Assert.IsNotNull(typeUnderTest, string.Format("{0}the unit test class must specify the type under test by deriving from UnitTest<T> or setting the type in the constructor.", testErrorPrefix));
            Assert.AreNotEqual(typeof(void), typeUnderTest, string.Format("{0}the unit test class must set TypeUnderTest to a type other than void.", testErrorPrefix));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test class name must start with the name of its type under test and end in Tests.")]
        [UnitTestLevel(UnitTestLevel.NotReady)]
        private void TestIsNamedCorrectly()
        {
            string testName = this.GetType().Name;
            string typeUnderTestName = string.Empty;
            if (this.TypeUnderTest != null)
            {
                typeUnderTestName = this.TypeUnderTest.Name;
                if (this.TypeUnderTest.IsGenericType)
                {
                    int genericPos = typeUnderTestName.IndexOf('`');
                    if (genericPos >= 0)
                    {
                        typeUnderTestName = typeUnderTestName.Substring(0, genericPos);
                    }

                    typeUnderTestName += genericSuffixes[0];    // preferred suffix is "OfT"
                }
            }

            Assert.IsTrue(testName.EndsWith("Tests"), string.Format("{0}the unit test class name must end with 'Tests'.", testErrorPrefix));
            Assert.IsTrue(testName.StartsWith(typeUnderTestName), string.Format("{0}the unit test class name must start with '{1}'.", testErrorPrefix, typeUnderTestName));
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must have a TypeIsCorrect method.")]
        [UnitTestLevel(UnitTestLevel.NotReady)]
        private void HasTypeIsCorrect()
        {
            bool hasTypeIsCorrect = this.TestMethods.Any((m) => m.Name.Equals(typeIsCorrectMethodName, StringComparison.Ordinal));
            if (!hasTypeIsCorrect)
            {
                Assert.Fail(
                    string.Format(
                        "{0}the unit test class must contain a test method named TypeIsCorrect to validate the '{1}' type.",
                        testErrorPrefix,
                        this.TypeUnderTest != null ? this.TypeUnderTest.Name : "<null>"));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must have a valid [Description].")]
        [UnitTestLevel(UnitTestLevel.NotReady)]
        private void HasValidDescriptionAttribute()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MethodInfo methodInfo in this.TestMethods)
            {
                DescriptionAttribute description = methodInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
                if (description == null)
                {
                    sb.AppendLine(string.Format("    {0}: should have [Description].", methodInfo.Name));
                }
                else if (!description.Description.EndsWith("."))
                {
                    sb.AppendLine(string.Format("    {0}: [Description] does not end in period: <{1}>.", methodInfo.Name, description.Description));
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(string.Format("{0}these test methods have incorrect [Description] attributes:\r\n{1}.", testErrorPrefix, sb.ToString()));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("The unit test methods have the right method signature in [Description].")]
        [UnitTestLevel(UnitTestLevel.Complete)]
        private void AllMethodsHaveValidSignatureInDescriptionAttribute()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MethodInfo methodInfo in this.TestMethods.Where((m) => !m.Name.Equals(typeIsCorrectMethodName, StringComparison.OrdinalIgnoreCase)))
            {
                DescriptionAttribute description = methodInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().FirstOrDefault();
                if (description != null)
                {
                    IEnumerable<string> errors = this.GetErrorsInDescriptionSignature(methodInfo);
                    if (errors.Any())
                    {
                        string errorMessage = string.Join(Environment.NewLine + "    ", errors);
                        sb.AppendLine(string.Format("    {0}: [Description] has incorrect parameter signature: {1}.", methodInfo.Name, errorMessage));
                    }
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(string.Format("{0}these test methods in {1} have incorrect [Description] attributes:\r\n{2}.", testErrorPrefix, this.GetType().Name, sb.ToString()));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must have a valid [TestCategory].")]
        [UnitTestLevel(UnitTestLevel.NotReady)]
        private void HasValidTestCategoryAttribute()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MethodInfo methodInfo in this.TestMethods)
            {
                TestCategoryAttribute testCategory = methodInfo.GetCustomAttributes(typeof(TestCategoryAttribute), false).Cast<TestCategoryAttribute>().FirstOrDefault();
                if (testCategory == null)
                {
                    sb.AppendLine(string.Format("    {0}: should have [TestCategory].", methodInfo.Name));
                }
                else if (string.IsNullOrWhiteSpace(string.Join(" ", testCategory.TestCategories)))
                {
                    sb.AppendLine(string.Format("    {0}: [TestCategory] cannot be empty.", methodInfo.Name));
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(string.Format("{0}these test methods have incorrect [TestCategory] attributes:\r\n{1}.", testErrorPrefix, sb.ToString()));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must have a valid [Owner].")]
        [UnitTestLevel(UnitTestLevel.NotReady)]
        private void HasValidOwnerAttribute()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MethodInfo methodInfo in this.TestMethods)
            {
                OwnerAttribute owner = methodInfo.GetCustomAttributes(typeof(OwnerAttribute), false).Cast<OwnerAttribute>().FirstOrDefault();
                if (owner == null)
                {
                    sb.AppendLine(string.Format("    {0}: should have [Owner].", methodInfo.Name));
                }
                else if (string.IsNullOrWhiteSpace(owner.Owner))
                {
                    sb.AppendLine(string.Format("    {0}: [Owner] cannot be empty.", methodInfo.Name));
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(string.Format("{0}these test methods have incorrect [Owner] attributes:\r\n{1}.", testErrorPrefix, sb.ToString()));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must have a valid [Timeout].")]
        [UnitTestLevel(UnitTestLevel.InProgress)]
        private void HasValidTimeoutAttribute()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MethodInfo methodInfo in this.TestMethods)
            {
                TimeoutAttribute timeout = methodInfo.GetCustomAttributes(typeof(TimeoutAttribute), false).Cast<TimeoutAttribute>().FirstOrDefault();
                if (timeout == null)
                {
                    sb.AppendLine(string.Format("    {0}: should have [Timeout].", methodInfo.Name));
                }
                else if (timeout.Timeout <= 0 || timeout.Timeout > (TimeoutConstant.ExtendedTimeout))
                {
                    sb.AppendLine(string.Format("    {0}: [Timeout({1})] must be > 0 and < {2} milliseconds.", methodInfo.Name, timeout.Timeout, TimeoutConstant.ExtendedTimeout));
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(string.Format("{0}these test methods have incorrect [Timeout] attributes:\r\n{1}.", testErrorPrefix, sb.ToString()));
            }
        }
      
        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("This unit test must not use [ExpectedException].")]
        [UnitTestLevel(UnitTestLevel.InProgress)]
        private void DoesNotUseExpectedException()
        {
            StringBuilder sb = new StringBuilder();
            foreach (MethodInfo methodInfo in this.TestMethods)
            {
                if (methodInfo.GetCustomAttributes(typeof(ExpectedExceptionAttribute), false).Any())
                {
                    sb.AppendLine(string.Format("    {0}", methodInfo.Name));
                }
            }

            if (sb.Length != 0)
            {
                Assert.Fail(string.Format("{0}these unit test methods use [ExpectedException].  They should use ExceptionAssert instead:\r\n{1}", testErrorPrefix, sb.ToString()));
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("All visible members of the type under test have a test.")]
        [UnitTestLevel(UnitTestLevel.Complete)]
        private void AllMembersAreTested()
        {
            StringBuilder sb = new StringBuilder();
            string untestedMembers = this.GetSummaryOfUntestedMembers();

            if (!string.IsNullOrWhiteSpace(untestedMembers))
            {
                Assert.Fail(string.Format("{0}'{1}", testErrorPrefix, untestedMembers));
            }
        }

        private string GetSummaryOfUntestedMembers()
        {
            StringBuilder sb = new StringBuilder();
            List<string> errorMessages = new List<string>();

            Type typeUnderTest = this.TypeUnderTest;
            Assert.IsNotNull(typeUnderTest, string.Format("{0}the unit test class must specify the type under test by deriving from UnitTest<T> or setting the type in the constructor.", testErrorPrefix));

            MemberInfo[] untestedMembers = this.GetListOfUntestedMembers(errorMessages);
            foreach (ConstructorInfo constructor in untestedMembers.OfType<ConstructorInfo>())
            {
                sb.AppendLine(string.Format("    constructor: {0}", this.GenerateMethodOrConstructorName(constructor)));
            }

            foreach (PropertyInfo property in untestedMembers.OfType<PropertyInfo>())
            {
                sb.AppendLine(string.Format("    property:   {0}", property.Name));
            }

            foreach (MethodInfo method in untestedMembers.OfType<MethodInfo>())
            {
                sb.AppendLine(string.Format("    method:    {0}", this.GenerateMethodOrConstructorName(method)));
            }

            string summary = string.Empty;

            if (sb.Length > 0)
            {
                summary = string.Format("'{0}' does not appear to test the following members:\r\n{1}", this.GetType().Name, sb.ToString());
            }

            if (errorMessages.Count > 0)
            {
                summary += string.Format("{0} contains the following [Description] errors:\r\n    {1}", this.GetType().Name, string.Join("\r\n    ", errorMessages));
            }

            return summary;
        }

        private MemberInfo[] GetListOfUntestedMembers(List<string> errorMessages)
        {
            List<MemberInfo> untested = new List<MemberInfo>();

            MemberInfo[] toTest = this.GetMembersToTest();
            HashSet<MemberInfo> tested = new HashSet<MemberInfo>(this.GetMembersTested(errorMessages));

            foreach (MemberInfo member in toTest)
            {
                if (!tested.Contains(member))
                {
                    untested.Add(member);
                }
            }

            return untested.ToArray();
        }

        private MemberInfo[] GetMembersTested(List<string> errorMessages)
        {
            List<MemberInfo> members = new List<MemberInfo>();

            foreach (MethodInfo testMethod in this.TestMethods)
            {
                MemberInfo memberInfo = GetMemberInfoFromTestMethod(this.TypeUnderTest, testMethod, errorMessages);
                if (memberInfo != null)
                {
                    members.Add(memberInfo);
                }
            }

            return members.ToArray();
        }

        private MemberInfo[] GetMembersToTest()
        {
            List<MemberInfo> members = new List<MemberInfo>();

            // static ctors ignored
            ConstructorInfo[] constructors = typeUnderTest.GetConstructors(publicOrPrivateInstance);
            foreach (ConstructorInfo constructor in constructors.Where((m) => !m.IsPrivate && m.DeclaringType == typeUnderTest))
            {
                members.Add(constructor);
            }

            MethodInfo[] methods = typeUnderTest.GetMethods(publicOrPrivateStaticOrInstance);
            foreach (MethodInfo method in methods.Where((m) => !m.IsPrivate && m.DeclaringType == typeUnderTest && !m.IsSpecialName))
            {
                members.Add(method);
            }

            PropertyInfo[] properties = typeUnderTest.GetProperties(publicOrPrivateStaticOrInstance);
            foreach (PropertyInfo property in properties.Where((p) => p.DeclaringType == typeUnderTest))
            {
                // Don't require testing of private properties
                if ((property.GetGetMethod() != null && !property.GetGetMethod().IsPrivate) ||
                    (property.GetSetMethod() != null && !property.GetSetMethod().IsPrivate))
                {
                    members.Add(property);
                }
            }

            return members.ToArray();
        }

        private IEnumerable<string> GetErrorsInDescriptionSignature(MethodInfo testMethod)
        {
            List<string> errorMessages = new List<string>();
            MemberInfo memberInfo = GetMemberInfoFromTestMethod(this.TypeUnderTest, testMethod, errorMessages);
            return errorMessages;
        }

        private string GenerateMethodOrConstructorName(MethodBase methodBase)
        {
            string name = (methodBase is ConstructorInfo) ? this.TypeUnderTest.Name : methodBase.Name;

            return string.Format("{0}({1})", name, GenerateParameterList(methodBase));
        }

        private static string GenerateParameterList(MethodBase methodBase)
        {
            StringBuilder sb = new StringBuilder();

            ParameterInfo[] parameters = methodBase.GetParameters();
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(parameters[i].ParameterType.Name);
            }

            return sb.ToString();
        }

        private static MemberInfo GetMemberInfoFromTestMethod(Type typeUnderTest, MethodInfo testMethod, List<string> errorMessages)
        {
            DescriptionAttribute descriptionAttribute = testMethod.GetCustomAttributes(typeof(DescriptionAttribute), false).Cast<DescriptionAttribute>().SingleOrDefault();
            if (descriptionAttribute == null)
            {
                errorMessages.Add("[Description] is missing.");
                return null;
            }
            string description = descriptionAttribute.Description;

            int openParen = description.IndexOf('(');
            int firstSpace = description.IndexOf(' ');
            int endOfName = (openParen > 0 && firstSpace > 0)
                                ? Math.Min(openParen, firstSpace)
                                : openParen > 0
                                    ? openParen
                                    : firstSpace;
            if (endOfName < 0)
            {
                endOfName = description.Length - 1;
            }

            string memberName = description.Substring(0, endOfName).Trim();

            string baseName;
            bool isGenericName = TryMatchAsGenericName(memberName, out baseName);
            if (isGenericName)
            {
                memberName = baseName;
            }

            // Normalize generic type names to "Xxx<T>" form to match ctors below
            string normalizedTypeName = GetTypeNameWithoutGeneric(typeUnderTest);

            // Special case TypeIsCorrect to return the TypeInfo
            if (testMethod.Name.Equals(typeIsCorrectMethodName))
            {
                if (!normalizedTypeName.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessages.Add("The TypeIsCorrect [Description] must name the type under test.");
                }

                return typeUnderTest;
            }

            if (endOfName == openParen)
            {
                int closeParen = description.IndexOf(')', openParen + 1);
                if (closeParen < 0)
                {
                    errorMessages.Add(string.Format("Missing ')' in signature in [Description] for {0}", testMethod.Name));
                    return null;
                }

                string parametersString = description.Substring(openParen + 1, closeParen - openParen - 1);
                parametersString = parametersString.Trim();

                string[] parameterList = string.IsNullOrWhiteSpace(parametersString)
                                            ? new string[0]
                                            : parametersString.IndexOf(',') < 0
                                                ? new string[] { parametersString }
                                                : parametersString.Split(',');

                for (int i = 0; i < parameterList.Length; ++i)
                {
                    parameterList[i] = parameterList[i].Trim();
                }

                if (string.Equals(normalizedTypeName, memberName, StringComparison.OrdinalIgnoreCase))
                {
                    // Static ctors ignored in current implementation
                    ConstructorInfo[] constructors = typeUnderTest.GetConstructors(publicOrPrivateInstance);

                    // Accept empty parameter list if only overload
                    if (constructors.Length == 1 && parameterList.Length == 0)
                    {
                        return constructors[0];
                    }

                    foreach (ConstructorInfo constructor in constructors)
                    {
                        ParameterInfo[] parameters = constructor.GetParameters();
                        if (parameters.Length == parameterList.Length)
                        {
                            int matchCount = 0;
                            for (int i = 0; i < parameters.Length; ++i)
                            {
                                // Allow "T" to match any type for generic types
                                if ((typeUnderTest.IsGenericType && parameterList[i].Equals("T", StringComparison.OrdinalIgnoreCase)) ||
                                    MatchParameterType(parameterList[i], parameters[i].ParameterType))
                                {
                                    ++matchCount;
                                }
                            }

                            if (matchCount == parameters.Length)
                            {
                                return constructor;
                            }
                        }
                    }

                    errorMessages.Add(
                        string.Format(
                            "There is no ctor with the signature '{0}' specfied in [Description] for {1}.", 
                            string.IsNullOrWhiteSpace(parametersString) ? "<empty>" : parametersString,
                            testMethod.Name));

                    return null;
                }

                else
                {
                    MethodInfo[] methods = typeUnderTest.GetMethods(publicOrPrivateStaticOrInstance).Where((m) => m.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (methods.Length == 0)
                    {
                        errorMessages.Add(string.Format("Has no method named '{0}' specified in [Description] for {1}.", memberName, testMethod.Name));
                        return null;
                    }

                    // Accept empty parameter list if only overload
                    if (methods.Length == 1 && parameterList.Length == 0)
                    {
                        return methods[0];
                    }

                    foreach (MethodInfo method in methods)
                    {
                        bool thisMethodIsGeneric = method.IsGenericMethod;

                        // If name was XxxOfT, we must match generics only
                        if (thisMethodIsGeneric != isGenericName)
                        {
                            continue;
                        }

                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == parameterList.Length)
                        {
                            int matchCount = 0;
                            for (int i = 0; i < parameters.Length; ++i)
                            {
                                // Allow "T" to match any type for generic types
                                if ((typeUnderTest.IsGenericType && parameterList[i].Equals("T", StringComparison.OrdinalIgnoreCase)) ||
                                    MatchParameterType(parameterList[i], parameters[i].ParameterType))
                                {
                                    ++matchCount;
                                }
                            }

                            if (matchCount == parameters.Length)
                            {
                                return method;
                            }
                        }
                    }

                    errorMessages.Add(string.Format(
                        "No overload of '{0}.{1}' has the parameter list '{2}' specified in [Description] for {3}.",
                        typeUnderTest.Name,
                        memberName,
                        string.IsNullOrWhiteSpace(parametersString) ? "<empty>" : parametersString,
                        testMethod.Name));

                    return null;
                }
            }
            else
            {
                foreach (PropertyInfo propertyInfo in typeUnderTest.GetProperties(publicOrPrivateStaticOrInstance))
                {
                    if (memberName.Equals(propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return propertyInfo;
                    }
                }

                errorMessages.Add(string.Format("'{0}' is not a valid property name.", memberName));
                return null;
            }
        }

        private static bool MatchParameterType(string parameterName, Type parameterType)
        {
            string[] parameterNameParts = parameterName.Split(' ');
            if (parameterNameParts.Length > 1)
            {
                parameterName = parameterNameParts[parameterNameParts.Length-1];
            }

            if (string.Equals(parameterName, parameterType.Name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (parameterType.IsGenericType)
            {
                string genericBaseName = parameterType.Name.Substring(0, parameterType.Name.IndexOf('`'));
                string genericTypeName = string.Format("{0}<{1}>", genericBaseName, parameterType.GetGenericArguments()[0].Name);
                return string.Equals(parameterName, genericTypeName, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private static string GetTypeNameWithoutGeneric(Type type)
        {
            string name = type.Name;
            if (type.IsGenericType)
            {
                int genericPos = name.IndexOf('`');
                if (genericPos >= 0)
                {
                    name = name.Substring(0, genericPos);
                }
            }

            return name;
        }

        private static MethodInfo[] GetTestMethods(Type unitTestClassType)
        {
            Assert.IsNotNull(unitTestClassType, "UnitTestClassType cannot be null.");

            return unitTestClassType.GetMethods(publicDeclaredInstanceBindingFlags)
                    .Where((m) => m.GetCustomAttributes(typeof(TestMethodAttribute), false).Any() &&
                                  !m.GetCustomAttributes(typeof(IgnoreAttribute), false).Any())
                    .OrderBy((m) => m.Name)
                    .ToArray();
        }

        private static bool TryMatchAsGenericName(string name, out string baseName)
        {
            foreach (string suffix in genericSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    baseName = name.Remove(name.Length - suffix.Length);
                    return true;
                }
            }

            baseName = name;
            return false;
        }

        private static UnitTestLevel UnitTestLevelOfMember(MemberInfo memberInfo)
        {
            UnitTestLevelAttribute unitTestLevelAttribute =
                memberInfo.GetCustomAttributes(typeof(UnitTestLevelAttribute), false)
                    .Cast<UnitTestLevelAttribute>()
                        .SingleOrDefault();

            return unitTestLevelAttribute == null ? UnitTestLevel.None : unitTestLevelAttribute.UnitTestLevel;
        }
    }

    /// <summary>
    /// Generic form of <see cref="UnitTest"/> where the generic parameter <typeparamref name="T"/>
    /// describes the type under test.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class UnitTest<T> : UnitTest
    {
    }
}
