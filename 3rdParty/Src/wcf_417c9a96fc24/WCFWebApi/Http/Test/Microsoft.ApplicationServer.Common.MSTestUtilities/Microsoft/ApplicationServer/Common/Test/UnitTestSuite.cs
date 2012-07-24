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
    /// Base class that validates all types in a specified assembly
    /// have correct unit test classes.
    /// </summary>
    /// <remarks>
    /// Unit test suite-level classes should derive from this class and call the
    /// <see cref="UnitTestSuiteIsCorrect"/> to self-validate using MSTest.
    /// </remarks>
    [TestClass]
    public abstract class UnitTestSuite
    {
        private static readonly string testErrorPrefix = "Test suite error: ";
        private const BindingFlags publicDeclaredInstanceBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        private const BindingFlags publicOrPrivateDeclaredInstanceBindingFlags = publicDeclaredInstanceBindingFlags | BindingFlags.NonPublic;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitTestSuite"/> class.
        /// </summary>
        /// <param name="typeUnderTest">The type the unit test class will test.</param>
        protected UnitTestSuite(Assembly assemblyUnderTest)
        {
            this.AssemblyUnderTest = assemblyUnderTest;
        }

        /// <summary>
        /// Gets the <see cref="Assembly"/> being tested.
        /// </summary>
        protected Assembly AssemblyUnderTest { get; set; }

        /// <summary>
        /// Gets the <see cref="UnitTestLevel"/> at which this assembly should be validated.
        /// </summary>
        protected UnitTestLevel UnitTestLevel
        {
            get
            {
                return UnitTestLevelOfMember(this.GetType());
            }
        }

        /// <summary>
        /// Subclasses are required to implement this, and should call <see cref="ValidateUnitTestSuite"/>.
        /// </summary>
        public abstract void UnitTestSuiteIsCorrect();

        /// <summary>
        /// Validates all product types in this suite's assembly adhere to a common set of conventions.
        /// </summary>
        /// <remarks>
        /// This method discovers and executes all the test methods in this current <see cref="UnitTestSuite"/>
        /// base class.  To add more validation, simply add more test methods to this base class.
        /// Please mark the test methods private so that they cannot be inherited by the derived tests.
        /// </remarks>
        protected void ValidateUnitTestSuite()
        {
            UnitTestLevel classLevel = UnitTestLevelOfMember(this.GetType());
            MethodInfo[] baseMethods = typeof(UnitTestSuite).GetMethods(publicOrPrivateDeclaredInstanceBindingFlags)
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
                string missingTests;
                switch (this.UnitTestLevel)
                {
                    case UnitTestLevel.None:
                        System.Diagnostics.Debug.WriteLine(
                            string.Format(
                                "'{0}' is not yet enabled for unit test suite verification.\r\nAdd [UnitTestLevel(UnitTestLevel.NotReady)] to the class to enable basic verification.",
                                this.GetType().Name));
                        break;

                    case UnitTestLevel.NotReady:
                        missingTests = this.GetSummaryOfTypesNotTested();
                        if (!string.IsNullOrWhiteSpace(missingTests))
                        {
                            System.Diagnostics.Debug.WriteLine(missingTests);
                        }

                        break;

                    case UnitTestLevel.InProgress:
                        missingTests = this.GetSummaryOfTypesNotTested();
                        if (!string.IsNullOrWhiteSpace(missingTests))
                        {
                            System.Diagnostics.Debug.WriteLine(missingTests);
                        }
                        
                        break;

                    case UnitTestLevel.Complete:
                        break;
                }
            }
        }

        [TestMethod]
        [TestCategory("CIT"), Timeout(TimeoutConstant.DefaultTimeout), Owner("vinelap")]
        [Description("All types in this assembly have tests.")]
        [UnitTestLevel(UnitTestLevel.Complete)]
        private void AllTypesHaveTests()
        {
            Assert.IsNotNull(this.AssemblyUnderTest, "AssemblyUnderTest was not defined.");
            string missingTests = this.GetSummaryOfTypesNotTested();

            if (!string.IsNullOrWhiteSpace(missingTests))
            {
                Assert.Fail(string.Format("{0} {1}", testErrorPrefix, missingTests.ToString()));
            }
        }

        private string GetSummaryOfTypesNotTested()
        {
            Assert.IsNotNull(this.AssemblyUnderTest, "AssemblyUnderTest was not defined.");
            StringBuilder sb = new StringBuilder();
            IEnumerable<Type> typesNotTested = this.GetTypesNotTested();

            foreach (Type t in typesNotTested)
            {
                sb.AppendLine(string.Format("    {0}", t.Name));
            }

            if (sb.Length != 0)
            {
                return string.Format("The following types in {0} do not appear to have unit tests:\r\n{1}", this.AssemblyUnderTest.GetName().Name, sb.ToString());
            }

            return string.Empty;
        }

        private IEnumerable<Type> GetTypesNotTested()
        {
            Assert.IsNotNull(this.AssemblyUnderTest, "AssemblyUnderTest was not defined.");
            List<Type> result = new List<Type>();

            Assembly testAssembly = this.GetType().Assembly;
            Type[] testTypes = testAssembly.GetTypes();
            Type[] knownTestedTypes = this.TypesTested(testTypes);

            Type[] productTypes = this.AssemblyUnderTest.GetTypes();
            Type[] productTypesNotMarkedTested = productTypes.Where((t) => !knownTestedTypes.Contains(t)).ToArray();

            foreach (Type productType in productTypesNotMarkedTested)
            {
                if (productType.IsPublic)
                {
                    if (productType.IsGenericType)
                    {
                        // Generic type tests generally are marked as Xxx<object>
                        if (knownTestedTypes.Any((t) => t.IsGenericType && t.GetGenericTypeDefinition() == productType))
                        {
                            continue;
                        }
                    }

                    string testNamePrefix = AsTestName(productType);
                    if (!testTypes.Any((t) => t.Name.StartsWith(testNamePrefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add(productType);
                    }
                }
            }

            return result;
        }


        private Type[] TypesTested(Type[] testClassTypes)
        {
            List<Type> types = new List<Type>();
            foreach (Type testClassType in testClassTypes)
            {
                Type typeUnderTest = UnitTest.GetTypeUnderTest(testClassType);
                if (typeUnderTest != null)
                {
                    types.Add(typeUnderTest);
                }
            }

            return types.ToArray();
        }

        private static string AsTestName(Type t)
        {
            string s = t.Name;
            int genericChar = s.IndexOf('`');
            if (genericChar >= 0)
            {
                s = s.Substring(0, genericChar) + "OfT";
            }

            return s;
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
}
