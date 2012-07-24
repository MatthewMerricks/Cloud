namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Json;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.VisualStudio.TestTools.UnitTesting;    
    
    /// <summary>
    ///This is a test class for JsonValueDynamicMetaObjectTest and is intended to perform sanity tests on this class.
    ///Extended tests are performed by the JsonValue dynamic feature tests.
    ///</summary>
    [TestClass()]
    public class JsonValueDynamicMetaObjectTest
    {
        const string NonSingleNonNullIndexNotSupported = "Null index or multidimensional indexing is not supported by this indexer; use 'System.Int32' or 'System.String' for array and object indexing respectively.";
            
        /// <summary>
        /// A test for GetMetaObject
        ///</summary>
        [TestMethod()]
        public void GetMetaObjectTest()
        {
            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var target = GetJsonValueDynamicMetaObject(AnyInstance.AnyJsonObject, null); });
        }

        /// <summary>
        /// A test for BindInvokeMember
        ///</summary>
        [TestMethod()]
        public void BindInvokeMemberTest()
        {
            JsonValue value = AnyInstance.AnyJsonValue1;
            DynamicMetaObject target = GetJsonValueDynamicMetaObject(value);

            TestInvokeMemberBinder.TestBindParams(target);

            string methodName;
            object[] arguments;
            object result = null;

            methodName = "ToString";
            arguments = new object[] { };
            TestInvokeMemberBinder.TestMetaObject(target, methodName, arguments);

            methodName = "TryReadAs";
            arguments = new object[] { typeof(int), result };
            TestInvokeMemberBinder.TestMetaObject(target, methodName, arguments);

            methodName = "TryReadAsType";
            arguments = new object[] { typeof(Person), result };
            TestInvokeMemberBinder.TestMetaObject(target, methodName, arguments, true);
        }
 
        /// <summary>
        /// A test for BindConvert
        ///</summary>
        [TestMethod()]
        public void BindConvertTest()
        {
            JsonValue value;
            DynamicMetaObject target;

            value = (JsonValue)AnyInstance.AnyInt;
            target = GetJsonValueDynamicMetaObject(value);
            TestConvertBinder.TestBindParams(target);

            Type[] intTypes = { typeof(int), typeof(uint), typeof(long), };

            foreach (Type type in intTypes)
            {
                TestConvertBinder.TestMetaObject(target, type);
            }

            value = (JsonValue)AnyInstance.AnyString;
            target = GetJsonValueDynamicMetaObject(value);
            TestConvertBinder.TestMetaObject(target, typeof(string));

            value = (JsonValue)AnyInstance.AnyJsonValue1;
            target = GetJsonValueDynamicMetaObject(value);
            TestConvertBinder.TestMetaObject(target, typeof(JsonValue));
            TestConvertBinder.TestMetaObject(target, typeof(IEnumerable<KeyValuePair<string, JsonValue>>));
            TestConvertBinder.TestMetaObject(target, typeof(IDynamicMetaObjectProvider));
            TestConvertBinder.TestMetaObject(target, typeof(object));

            TestConvertBinder.TestMetaObject(target, typeof(Person), false);
        }

        /// <summary>
        /// A test for BindGetIndex
        ///</summary>
        [TestMethod()]
        public void BindGetIndexTest()
        {
            JsonValue value = AnyInstance.AnyJsonArray;

            DynamicMetaObject target = GetJsonValueDynamicMetaObject(value);

            TestGetIndexBinder.TestBindParams(target);

            foreach (KeyValuePair<string, JsonValue> pair in value)
            {
                TestGetIndexBinder.TestMetaObject(target, int.Parse(pair.Key)); 
            }
        }

        /// <summary>
        /// A test for BindSetIndex
        ///</summary>
        [TestMethod()]
        public void BindSetIndexTest()
        {
            JsonValue jsonValue = AnyInstance.AnyJsonArray;

            DynamicMetaObject target = GetJsonValueDynamicMetaObject(jsonValue);

            TestSetIndexBinder.TestBindParams(target);

            int value = 0;

            foreach (KeyValuePair<string, JsonValue> pair in jsonValue)
            {
                TestSetIndexBinder.TestMetaObject(target, int.Parse(pair.Key), value++);
            }
        }

        /// <summary>
        /// A test for BindGetMember.
        ///</summary>
        [TestMethod()]
        public void BindGetMemberTest()
        {
            JsonValue value = AnyInstance.AnyJsonObject;

            DynamicMetaObject target = GetJsonValueDynamicMetaObject(value);

            TestGetMemberBinder.TestBindParams(target);

            foreach (KeyValuePair<string, JsonValue> pair in value)
            {
                TestGetMemberBinder.TestMetaObject(target, pair.Key);
            }
        }

        /// <summary>
        /// A test for BindSetMember.
        ///</summary>
        [TestMethod()]
        public void BindSetMemberTest()
        {
            JsonValue value = AnyInstance.AnyJsonObject;

            string expectedMethodSignature = "System.Json.JsonValue SetValue(System.String, System.Object)";

            DynamicMetaObject target = GetJsonValueDynamicMetaObject(value);
            DynamicMetaObject arg = new DynamicMetaObject(Expression.Parameter(typeof(int)), BindingRestrictions.Empty, AnyInstance.AnyInt);

            TestSetMemberBinder.TestBindParams(target, arg);

            foreach (KeyValuePair<string, JsonValue> pair in value)
            {
                TestSetMemberBinder.TestMetaObject(target, pair.Key, arg, expectedMethodSignature);
            }
        }

        /// <summary>
        /// A test for BindUnaryOperation.
        ///</summary>
        [TestMethod()]
        public void BindUnaryOperationTest()
        {
            JsonValue value;
            DynamicMetaObject target;

            value =  new JsonPrimitive(AnyInstance.AnyInt);
            target = GetJsonValueDynamicMetaObject(value);

            TestUnaryOperationBinder.TestBindParams(target);
            TestUnaryOperationBinder.TestMetaObject(target, TestUnaryOperationBinder.NumberOperations);

            value = new JsonPrimitive(AnyInstance.AnyBool);
            target = GetJsonValueDynamicMetaObject(value);

            TestUnaryOperationBinder.TestMetaObject(target, TestUnaryOperationBinder.BooleanOperations);
        }

        /// <summary>
        ///An test for BindUnaryOperation with invalid input.
        ///</summary>
        [TestMethod()]
        public void InvalidBindUnaryOperationTest()
        {
            JsonValue[] values = 
            {
                AnyInstance.AnyJsonObject,
                AnyInstance.AnyJsonArray,
                AnyInstance.AnyInt,
                AnyInstance.DefaultJsonValue
            };

            foreach (JsonValue value in values)
            {
                DynamicMetaObject target = GetJsonValueDynamicMetaObject(value);

                TestUnaryOperationBinder.TestMetaObject(target, TestUnaryOperationBinder.UnsupportedOperations, false);

                if (!(value is JsonPrimitive))
                {
                    TestUnaryOperationBinder.TestMetaObject(target, TestUnaryOperationBinder.NumberOperations, false);
                    TestUnaryOperationBinder.TestMetaObject(target, TestUnaryOperationBinder.BooleanOperations, false);
                }
            }
        }

        /// <summary>
        /// A test for BindBinaryOperation.
        ///</summary>
        [TestMethod()]
        public void BindBinaryOperationTest()
        {
            //// Binary operators: +, -, *, /, %, &, |, ^, <<, >>, ==, !=, >, <, >=, <=
            //// The '&&' and '||' operators are conditional versions of the '&' and '|' operators.

            JsonValue value;
            DynamicMetaObject target;
            DynamicMetaObject arg;

            value = AnyInstance.AnyInt;
            target = GetJsonValueDynamicMetaObject(value);
            arg = new DynamicMetaObject(Expression.Constant(AnyInstance.AnyInt), BindingRestrictions.Empty, AnyInstance.AnyInt);

            TestBinaryOperationBinder.TestBindParams(target, arg);
            TestBinaryOperationBinder.TestMetaObject(target, arg, TestBinaryOperationBinder.NumberOperations);

            value = AnyInstance.AnyBool;
            target = GetJsonValueDynamicMetaObject(value);
            arg = new DynamicMetaObject(Expression.Constant(AnyInstance.AnyBool), BindingRestrictions.Empty, AnyInstance.AnyBool);
            TestBinaryOperationBinder.TestMetaObject(target, arg, TestBinaryOperationBinder.BoolOperations);

            arg = target;
            foreach (JsonValue otherValue in AnyInstance.AnyJsonValueArray)
            {
                if (!(otherValue is JsonPrimitive))
                {
                    target = GetJsonValueDynamicMetaObject(otherValue);
                    TestBinaryOperationBinder.TestMetaObject(target, arg, TestBinaryOperationBinder.CompareOperations);
                }
            }

            arg = new DynamicMetaObject(Expression.Constant(null), BindingRestrictions.Empty);
            foreach (JsonValue otherValue in AnyInstance.AnyJsonValueArray)
            {
                target = GetJsonValueDynamicMetaObject(otherValue);
                TestBinaryOperationBinder.TestMetaObject(target, arg, TestBinaryOperationBinder.CompareOperations);
            }
        }

        /// <summary>
        /// A test for BindBinaryOperation with invalid input.
        ///</summary>
        [TestMethod()]
        public void BindBinaryOperationInvalidTest()
        {
            DynamicMetaObject target;

            DynamicMetaObject argPrimitive = new DynamicMetaObject(Expression.Constant(10), BindingRestrictions.Empty);
            DynamicMetaObject argNonPrimitive = new DynamicMetaObject(Expression.Constant(AnyInstance.AnyJsonObject), BindingRestrictions.Empty);

            foreach (JsonValue value in AnyInstance.AnyJsonValueArray)
            {
                target = GetJsonValueDynamicMetaObject(value);

                TestBinaryOperationBinder.TestMetaObject(target, argPrimitive, TestBinaryOperationBinder.UnsupportedOperations, false);
            }

            foreach (JsonValue value in AnyInstance.AnyJsonValueArray)
            {
                target = GetJsonValueDynamicMetaObject(value);

                if (value is JsonPrimitive)
                {
                    //// not supported on operand of type JsonValue if it isn't a primitive and not comparing JsonValue types.
                    TestBinaryOperationBinder.TestMetaObject(target, argNonPrimitive, TestBinaryOperationBinder.NumberOperations, false);
                    TestBinaryOperationBinder.TestMetaObject(target, argNonPrimitive, TestBinaryOperationBinder.BoolOperations, false);
                }
                else
                {
                    //// When value is non-primitive, it must be a compare operation and (the other operand must be null or a JsonValue)
                    TestBinaryOperationBinder.TestMetaObject(target, argPrimitive, TestBinaryOperationBinder.NumberOperations, false);
                    TestBinaryOperationBinder.TestMetaObject(target, argPrimitive, TestBinaryOperationBinder.BoolOperations, false);
                }
            }
        }

        /// <summary>
        /// A test for GetDynamicMemberNames
        ///</summary>
        [TestMethod()]
        public void GetDynamicMemberNamesTest()
        {
            JsonValue[] values = AnyInstance.AnyJsonValueArray;

            foreach (JsonValue value in values)
            {
                DynamicMetaObject target = GetJsonValueDynamicMetaObject(value);

                List<string> expected = new List<string>();
                foreach (KeyValuePair<string, JsonValue> pair in value)
                {
                    expected.Add(pair.Key);
                }

                IEnumerable<string> retEnumerable = target.GetDynamicMemberNames();
                Assert.IsNotNull(retEnumerable);

                List<string> actual = new List<string>(retEnumerable);
                Assert.AreEqual(expected.Count, actual.Count);

                for (int i = 0; i < expected.Count; i++)
                {
                    Assert.AreEqual<string>(expected[i], actual[i]);
                }
            }
        }

        /// <summary>
        /// Helper method for getting a <see cref="JsonValueDynamicMetaObject"/>.
        /// </summary>
        /// <param name="jsonValue">The <see cref="JsonValue"/>instance to get the dynamic meta-object from.</param>
        /// <returns></returns>
        private static DynamicMetaObject GetJsonValueDynamicMetaObject(JsonValue jsonValue)
        {
            return GetJsonValueDynamicMetaObject(jsonValue, Expression.Parameter(typeof(object)));
        }
        private static DynamicMetaObject GetJsonValueDynamicMetaObject(JsonValue jsonValue, Expression expression)
        {
            return ((IDynamicMetaObjectProvider)jsonValue).GetMetaObject(expression);
        }

        /// <summary>
        /// Test binder for method call operation.
        /// </summary>
        private class TestInvokeMemberBinder : InvokeMemberBinder
        {
            public TestInvokeMemberBinder(string name, int argCount)
                : base(name, false, new CallInfo(argCount, new string[]{}))
            { 
            }
            
            public static void TestBindParams(DynamicMetaObject target)
            {
                string methodName = "ToString";
                object[] arguments = new object[] { };

                InvokeMemberBinder binder = new TestInvokeMemberBinder(methodName, arguments.Length);
                DynamicMetaObject[] args = new DynamicMetaObject[arguments.Length];

                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindInvokeMember(null, args); });
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindInvokeMember(binder, null); });
            }

            public static void TestMetaObject(DynamicMetaObject target, string methodName, object[] arguments, bool isExtension = false)
            {
                InvokeMemberBinder binder = new TestInvokeMemberBinder(methodName, arguments.Length);
                DynamicMetaObject[] args = new DynamicMetaObject[arguments.Length];

                for (int idx = 0; idx < args.Length; idx++)
                {
                    object value = arguments[idx];
                    Type valueType = value != null ? value.GetType() : typeof(object);
                    args[idx] = new DynamicMetaObject(Expression.Parameter(valueType), BindingRestrictions.Empty, value);
                }

                DynamicMetaObject result = target.BindInvokeMember(binder, args);
                Assert.IsNotNull(result);

                if (isExtension)
                {
                    UnaryExpression expression = result.Expression as UnaryExpression;
                    Assert.IsNotNull(expression);

                    MethodCallExpression callExpression = expression.Operand as MethodCallExpression;
                    Assert.IsNotNull(callExpression);

                    Assert.IsTrue(callExpression.Method.ToString().Contains(methodName));
                }
                else
                {
                    Assert.AreSame(target, result.Value);
                }
            }

            public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }

            public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
            {
                // This is where the C# binder does the actual binding.
                return new DynamicMetaObject(Expression.Constant("FallbackInvokeMember called"), BindingRestrictions.Empty, target);
            }
        }

        /// <summary>
        /// The binder for the cast operation.
        /// </summary>
        private class TestConvertBinder : ConvertBinder 
        {
            public TestConvertBinder(Type type)
                : base(type, false)
            { 
            }

            public static void TestBindParams(DynamicMetaObject target)
            {
                ConvertBinder binder = new TestConvertBinder(typeof(int));
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindConvert(null); });
            }

            public static void TestMetaObject(DynamicMetaObject target, Type type, bool isValid = true)
            {
                ConvertBinder binder = new TestConvertBinder(type);
                DynamicMetaObject result = target.BindConvert(binder);
                Assert.IsNotNull(result);

                // Convert expression
                UnaryExpression expression = result.Expression as UnaryExpression;
                Assert.IsNotNull(expression);
                Assert.AreEqual<Type>(binder.Type, expression.Type);

                if (isValid)
                {
                    MethodCallExpression methodCallExp = expression.Operand as MethodCallExpression;

                    if (methodCallExp != null)
                    {
                        Assert.IsTrue(methodCallExp.Method.ToString().Contains("CastValue"));
                    }
                    else
                    {
                        ParameterExpression paramExpression = expression.Operand as ParameterExpression;
                        Assert.IsNotNull(paramExpression);
                    }
                }
                else
                {
                    Expression<Action> throwExp = Expression.Lambda<Action>(Expression.Block(expression), new ParameterExpression[] { });
                    ExceptionTestHelper.ExpectException<InvalidCastException>(() => throwExp.Compile().Invoke());
                }
            }

            public override DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Test binder for int indexer getter operation.
        /// </summary>
        private class TestGetIndexBinder : GetIndexBinder
        {
            public TestGetIndexBinder()
                : base(new CallInfo(0, new string[]{}))
            { 
            }

            public static void TestBindParams(DynamicMetaObject target)
            {
                GetIndexBinder binder = new TestGetIndexBinder();
                Expression typeExpression = Expression.Parameter(typeof(int));

                DynamicMetaObject[] indexes = 
                { 
                    new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 0), 
                    new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 1), 
                    new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 2) 
                };

                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindGetIndex(null, indexes); });
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindGetIndex(binder, null); });

                DynamicMetaObject[][] invalidIndexesParam =
                {
                    indexes,
                    new DynamicMetaObject[] { new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, null) },  
                    new DynamicMetaObject[] { null },
                    new DynamicMetaObject[] { }
                };

                foreach (DynamicMetaObject[] indexesParam in invalidIndexesParam)
                {
                    DynamicMetaObject metaObj = target.BindGetIndex(binder, indexesParam);

                    Expression<Action> expression = Expression.Lambda<Action>(Expression.Block(metaObj.Expression), new ParameterExpression[] { });
                    ExceptionTestHelper.ExpectException<ArgumentException>(() => { expression.Compile().Invoke(); }, NonSingleNonNullIndexNotSupported);
                }
            }
            
            public static void TestMetaObject(DynamicMetaObject target, int index, bool isValid = true)
            {
                string expectedMethodSignature = "System.Json.JsonValue GetValue(Int32)";

                GetIndexBinder binder = new TestGetIndexBinder();
                DynamicMetaObject[] indexes = {  new DynamicMetaObject(Expression.Parameter(typeof(int)), BindingRestrictions.Empty, index) };

                DynamicMetaObject result = target.BindGetIndex(binder, indexes);
                Assert.IsNotNull(result);

                MethodCallExpression expression = result.Expression as MethodCallExpression;
                Assert.IsNotNull(expression);
                Assert.AreEqual<string>(expectedMethodSignature, expression.Method.ToString());
            }

            public override DynamicMetaObject FallbackGetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Test binder for int indexer setter operation.
        /// </summary>
        private class TestSetIndexBinder : SetIndexBinder
        {
            public TestSetIndexBinder()
                : base(new CallInfo(0, new string[]{}))
            { 
            }

            public static void TestBindParams(DynamicMetaObject target)
            {
                SetIndexBinder binder = new TestSetIndexBinder();
                Expression typeExpression = Expression.Parameter(typeof(int));
                DynamicMetaObject[] indexes = new DynamicMetaObject[] { new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 0) };
                DynamicMetaObject value = new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, (JsonValue)10);

                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindSetIndex(null, indexes, value); });
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindSetIndex(binder, null, value); });
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindSetIndex(binder, indexes, null); });

                DynamicMetaObject[][] invalidIndexesParam =
                {
                    new DynamicMetaObject[]
                    { 
                        new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 0), 
                        new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 1), 
                        new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, 2) 
                    },

                    new DynamicMetaObject[] 
                    {
                        new DynamicMetaObject(typeExpression, BindingRestrictions.Empty, null)
                    },           

                    new DynamicMetaObject[]
                    {
                    }
                };

                foreach (DynamicMetaObject[] indexesParam in invalidIndexesParam)
                {
                    DynamicMetaObject metaObj = target.BindSetIndex(binder, indexesParam, value);

                    Expression<Action> expression = Expression.Lambda<Action>(Expression.Block(metaObj.Expression), new ParameterExpression[] { });
                    ExceptionTestHelper.ExpectException<ArgumentException>(() => { expression.Compile().Invoke(); }, NonSingleNonNullIndexNotSupported);
                }
            }

            public static void TestMetaObject(DynamicMetaObject target, int index, JsonValue jsonValue, bool isValid = true)
            {
                string expectedMethodSignature = "System.Json.JsonValue SetValue(Int32, System.Object)";

                SetIndexBinder binder = new TestSetIndexBinder();
                DynamicMetaObject[] indexes = { new DynamicMetaObject(Expression.Parameter(typeof(int)), BindingRestrictions.Empty, index) };
                DynamicMetaObject value = new DynamicMetaObject(Expression.Parameter(jsonValue.GetType()), BindingRestrictions.Empty, jsonValue);
                DynamicMetaObject result = target.BindSetIndex(binder, indexes, value);
                Assert.IsNotNull(result);

                MethodCallExpression expression = result.Expression as MethodCallExpression;
                Assert.IsNotNull(expression);
                Assert.AreEqual<string>(expectedMethodSignature, expression.Method.ToString());
            }

            public override DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Test binder for key indexer getter.
        /// </summary>
        private class TestGetMemberBinder : GetMemberBinder
        {
            public TestGetMemberBinder(string name)
                : base(name, false)
            { 
            }

            public static void TestBindParams(DynamicMetaObject target)
            {
                GetMemberBinder binder = new TestGetMemberBinder("AnyProperty");
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindGetMember(null); });
            }

            public static void TestMetaObject(DynamicMetaObject target, string name, bool isValid = true)
            {
                string expectedMethodSignature = "System.Json.JsonValue GetValue(System.String)";

                GetMemberBinder binder = new TestGetMemberBinder(name);

                DynamicMetaObject result = target.BindGetMember(binder);
                Assert.IsNotNull(result);

                MethodCallExpression expression = result.Expression as MethodCallExpression;
                Assert.IsNotNull(expression);
                Assert.AreEqual<string>(expectedMethodSignature, expression.Method.ToString());
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Test binder for key indexer setter.
        /// </summary>
        private class TestSetMemberBinder : SetMemberBinder
        {
            public TestSetMemberBinder(string name)
                : base(name, false)
            { 
            }

            public static void TestBindParams(DynamicMetaObject target, DynamicMetaObject value)
            {
                SetMemberBinder binder = new TestSetMemberBinder("AnyProperty");

                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindSetMember(null, value); });
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindSetMember(binder, null); });
            }

            public static void TestMetaObject(DynamicMetaObject target, string name, DynamicMetaObject value, string expectedMethodSignature, bool isValid = true)
            {
                SetMemberBinder binder = new TestSetMemberBinder(name);

                DynamicMetaObject result = target.BindSetMember(binder, value);
                Assert.IsNotNull(result);

                MethodCallExpression expression = result.Expression as MethodCallExpression;
                Assert.IsNotNull(expression);
                Assert.AreEqual<string>(expectedMethodSignature, expression.Method.ToString());
            }

            public override DynamicMetaObject FallbackSetMember(DynamicMetaObject target, DynamicMetaObject value, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Test binder for unary operations.
        /// </summary>
        private class TestUnaryOperationBinder : UnaryOperationBinder
        {
            public TestUnaryOperationBinder(ExpressionType operation)
                : base(operation)
            { 
            }
            public override DynamicMetaObject FallbackUnaryOperation(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }

            public static readonly ExpressionType[] NumberOperations = 
            {
                ExpressionType.UnaryPlus,
                ExpressionType.Negate,
                ExpressionType.OnesComplement,
            };

            public static readonly ExpressionType[] BooleanOperations = 
            {
                ExpressionType.Not,
                ExpressionType.IsFalse,
                ExpressionType.IsTrue
            };

            public static readonly ExpressionType[] UnsupportedOperations = 
            {
                ExpressionType.Increment,
                ExpressionType.Decrement,
            };

            public static void TestBindParams(DynamicMetaObject target)
            {
                UnaryOperationBinder binder = new TestUnaryOperationBinder(ExpressionType.Negate);
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindUnaryOperation(null); });
            }

            public static void TestMetaObject(DynamicMetaObject target, ExpressionType[] testExpressions, bool isValid = true)
            {
                foreach (ExpressionType expType in testExpressions)
                {
                    UnaryOperationBinder binder = new TestUnaryOperationBinder(expType);
                    DynamicMetaObject result = target.BindUnaryOperation(binder);
                    Assert.IsNotNull(result);

                    UnaryExpression expression = result.Expression as UnaryExpression;
                    Assert.IsNotNull(expression);

                    ExpressionType expectedType = isValid ? expType : ExpressionType.Throw;
                    Assert.AreEqual<ExpressionType>(expectedType, expression.Operand.NodeType);
                }
            }
        }

        /// <summary>
        /// Test binder for binary operations.
        /// </summary>
        private class TestBinaryOperationBinder : BinaryOperationBinder
        {
            public TestBinaryOperationBinder(ExpressionType operation)
                : base(operation)
            { 
            }

            public override DynamicMetaObject FallbackBinaryOperation(DynamicMetaObject target, DynamicMetaObject arg, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }


            public static readonly ExpressionType[] NumberOperations = 
            {
                ExpressionType.Add,
                ExpressionType.Subtract,
                ExpressionType.Multiply,
                ExpressionType.Divide,
                ExpressionType.Modulo,
                ExpressionType.LeftShift,
                ExpressionType.RightShift,
                ExpressionType.GreaterThan,
                ExpressionType.LessThan,
                ExpressionType.GreaterThanOrEqual,
                ExpressionType.LessThanOrEqual
            };

            public static readonly ExpressionType[] BoolOperations = 
            {
                ExpressionType.And,
                ExpressionType.Or,
                ExpressionType.ExclusiveOr,
            };

            public static readonly ExpressionType[] CompareOperations =
            {
                ExpressionType.Equal,
                ExpressionType.NotEqual
            };

            public static readonly ExpressionType[] UnsupportedOperations = 
            {
                ExpressionType.AddAssign,
                ExpressionType.SubtractAssign,
                ExpressionType.AndAssign,
                ExpressionType.OrAssign,
                ExpressionType.ExclusiveOrAssign,
                ExpressionType.MultiplyAssign,
                ExpressionType.DivideAssign,
                ExpressionType.ModuloAssign,
                ExpressionType.LeftShiftAssign,
                ExpressionType.RightShiftAssign
            };

            public static void TestBindParams(DynamicMetaObject target, DynamicMetaObject arg)
            {
                BinaryOperationBinder binder = new TestBinaryOperationBinder(ExpressionType.Equal);

                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindBinaryOperation(null, arg); });
                ExceptionTestHelper.ExpectException<ArgumentNullException>(() => { var result = target.BindBinaryOperation(binder, null); });
            }

            public static void TestMetaObject(DynamicMetaObject target, DynamicMetaObject arg, ExpressionType[] testExpressions, bool isValid = true)
            {
                foreach (ExpressionType expType in testExpressions)
                {
                    BinaryOperationBinder binder = new TestBinaryOperationBinder(expType);
                    DynamicMetaObject result = target.BindBinaryOperation(binder, arg);
                    Assert.IsNotNull(result);

                    //The resulting expression is a convert expression, that's why we are checking for unary expression here.
                    UnaryExpression expression = result.Expression as UnaryExpression;
                    Assert.IsNotNull(expression);

                    // The operand expression is supposed to be a binary expression.
                    ExpressionType expectedType = isValid ? expType : ExpressionType.Throw;
                    Assert.AreEqual<ExpressionType>(expectedType, expression.Operand.NodeType);
                }
            }
        }
    }
}
