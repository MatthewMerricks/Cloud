namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Json;
    using Microsoft.CSharp.RuntimeBinder;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class JsonValueDynamicOperatorOveloadTests
    {
        const string OperationNotDefinedMsgFormat = "Operation '{0}' is not defined for JsonValue instances of 'JsonType.{1}' type.";
        const string OperatorNotDefinedMsgFormat = "The binary operator {0} is not defined for the types '{1}' and '{2}'.";
        const string OperatorNotAllowedOnOperands = "Operation '{0}' cannot be applied on operands of type '{1}' and '{2}'.";

        [TestMethod]
        public void ComparisonOperatorsTest()
        {
            dynamic dyn1 = (JsonValue)3;
            dynamic dyn2 = (JsonValue)3;
            dynamic dyn3 = (JsonValue)"Hello";

            Assert.IsTrue(dyn1 != 5);
            Assert.IsTrue(dyn1 == 3);
            Assert.IsTrue(dyn1 > 2);
            Assert.IsTrue(dyn1 >= 3);
            Assert.IsTrue(dyn1 <= 5);
            Assert.IsTrue(dyn1 < 5);

            Assert.IsTrue(dyn1 == dyn2);
            Assert.IsTrue(dyn2 == dyn1);
            Assert.IsFalse(dyn1 < dyn2);
            Assert.IsFalse(dyn2 > dyn1);
            Assert.IsTrue(dyn1 <= dyn2);
            Assert.IsTrue(dyn2 >= dyn1);
        }

        [TestMethod]
        public void ShiftOperatorsTests()
        {
            uint number = 0x0010;
            dynamic dyn = (JsonValue)number;
            uint result;
            uint expected;

            result = dyn << 2;
            expected = number << 2;
            Assert.AreEqual(expected, result);

            result = dyn >> 1;
            expected = number >> 1;
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void InvalidShiftOperatorsTests()
        {
            dynamic dyn = (JsonValue)0x0010;

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn <<= 2; }, string.Format(OperationNotDefinedMsgFormat, "LeftShiftAssign", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn >>= 2; }, string.Format(OperationNotDefinedMsgFormat, "RightShiftAssign", "Number"));
        }

        [TestMethod]
        public void BitwiseOperatorsTests()
        {
            uint number = 0x0010;
            uint mask;
            uint expected;
            uint result;
            dynamic dyn = (JsonValue)number;

            mask = 0x0101;
            expected = number & mask;
            result = dyn & mask;
            Assert.AreEqual<uint>(expected, result);

            mask = 0x1001;
            expected = number | mask;
            result = dyn | mask;
            Assert.AreEqual<uint>(expected, result);

            expected = ~number;
            result = ~dyn;
            Assert.AreEqual<uint>(expected, result);

            expected = number ^ mask;
            result = dyn ^ mask;
            Assert.AreEqual<uint>(expected, result);
        }

        [TestMethod]
        public void InvalidBitwiseOperatorsTest()
        {
            dynamic dyn = (JsonValue)0x0010;

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn |= 0x0001; }, string.Format(OperationNotDefinedMsgFormat, "OrAssign", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn &= 0x0001; }, string.Format(OperationNotDefinedMsgFormat, "AndAssign", "Number"));
        }
        
        [TestMethod]
        public void ArithmeticOperatorsTest()
        {
            int number1 = 20;
            int number2 = 10;
            int resultI;
            int expectedI;

            dynamic dyn1 = (JsonValue)number1;
            dynamic dyn2 = (JsonValue)number2;

            expectedI = number1 + 2;
            resultI = dyn1 + 2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 - 1;
            resultI = dyn1 - 1;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 * 5;
            resultI = dyn1 * 5;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 / 2;
            resultI = dyn1 / 2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 / 3;
            resultI = dyn1 / 3;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 / 73;
            resultI = dyn1 / 73;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 % 2;
            resultI = dyn1 % 2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = -number1;
            resultI = -dyn1;
            Assert.AreEqual(expectedI, resultI);

            expectedI = +number1;
            resultI = +dyn1;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 + number2;
            resultI = dyn1 + dyn2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 - number2;
            resultI = dyn1 - dyn2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 * number2;
            resultI = dyn1 * dyn2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 / number2;
            resultI = dyn1 / dyn2;
            Assert.AreEqual(expectedI, resultI);

            expectedI = number1 % number2;
            resultI = dyn1 % dyn2;
            Assert.AreEqual(expectedI, resultI);
        }

        [TestMethod]
        public void InvalidArithmeticOperatorsTest()
        {
            dynamic dyn = (JsonValue)10;

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn += 2; ; }, string.Format(OperationNotDefinedMsgFormat, "AddAssign", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn -= 1; }, string.Format(OperationNotDefinedMsgFormat, "SubtractAssign", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn *= 1; }, string.Format(OperationNotDefinedMsgFormat, "MultiplyAssign", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn /= 1; }, string.Format(OperationNotDefinedMsgFormat, "DivideAssign", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn %= 1; }, string.Format(OperationNotDefinedMsgFormat, "ModuloAssign", "Number"));
        }

        [TestMethod]
        public void BooleanOperatorsTest()
        {
            dynamic dyn1 = (JsonValue)true;
            dynamic dyn2 = (JsonValue)false;
            bool result;

            result = dyn1 && false;
            Assert.IsFalse(result);

            result = dyn1 && dyn2;
            Assert.IsFalse(result);

            result = dyn1 || true;
            Assert.IsTrue(result);
            
            result = dyn1 || dyn2;
            Assert.IsTrue(result);

            result = !dyn1;
            Assert.IsFalse(result);
   
            bool ret = dyn1 ? true : false;
            Assert.IsTrue(ret);

            ret = dyn1 ?? false;
            Assert.IsTrue(ret);
        }

        [TestMethod]
        public void InvalidBooleanOperatorsTest()
        {
            dynamic dyn = (JsonValue)true;

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn &= false; }, string.Format(OperationNotDefinedMsgFormat, "AndAssign", "Boolean"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn |= false; }, string.Format(OperationNotDefinedMsgFormat, "OrAssign", "Boolean"));
        }

        [TestMethod]
        public void NotOperandSpecialTest()
        { 
            //// special-case for Not on int types since the DLR would change the operation to OnesComplement.

            dynamic target;

            target = (JsonValue)true;
            Assert.IsFalse(!target);

            target = (JsonValue)"false";
            Assert.IsTrue(!target);

            target = (JsonValue)AnyInstance.AnyInt;
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = !target; }, string.Format(OperationNotDefinedMsgFormat, "Not", "Number"));
        }

        [TestMethod]
        public void AsDynamicTest()
        {
            JsonValue jv2 = 3;

            Assert.IsTrue(jv2.AsDynamic() == 3);
            Assert.IsTrue(3 == (int)jv2.AsDynamic());
            Assert.IsTrue((JsonValue)3 == jv2.AsDynamic());

            ExceptionTestHelper.ExpectException<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(delegate { bool b = 3 == jv2.AsDynamic(); });
        }

        [TestMethod]
        public void JsonDefaultOperatorsTest()
        {
            JsonValue jv = 3;
            dynamic dyn = jv;
            dynamic defJv = dyn.IDontExist;

            Assert.IsFalse(dyn == null);
            Assert.IsFalse(null == dyn);
            Assert.IsTrue(dyn != null);
            Assert.IsTrue(null != dyn);

            Assert.IsTrue(defJv == null);
            Assert.IsFalse(defJv != null);
        }

        [TestMethod]
        public void JsonPrimitiveOperatorsTest()
        {
            int number = 10;
            int mask = 0x0001;
            int expected;
            int actual;

            dynamic jp1 = (JsonValue)number;
            dynamic jp2 = (JsonValue)number;
            dynamic jp3 = (JsonValue)jp1;

            Assert.IsTrue(jp1 == number);
            Assert.IsTrue( jp1 == jp2 );
            Assert.IsTrue( jp1 == jp3 );
            Assert.IsTrue( jp1 >= jp2 );
            Assert.IsTrue( jp1 <= jp3);
            Assert.IsFalse(jp1 > number);
            Assert.IsFalse(jp1 < jp2);
            Assert.IsFalse(jp1 != jp3);
            Assert.IsFalse(jp1 != number);

            expected = number * 10;
            actual = jp1 * 10;
            Assert.AreEqual<int>(expected, actual);

            expected = number / 10;
            actual = jp1 / 10;
            Assert.AreEqual<int>(expected, actual);

            expected = (int)(number ^ mask);
            actual = (int) (jp1 ^ 0x0001);
            Assert.AreEqual<int>(expected, actual);

            expected = (int)(number | mask);
            actual = (int)(jp1 | mask);
            Assert.AreEqual<int>(expected, actual);

            expected = (int)(number ^ mask);
            actual = (int)(jp1 ^ mask);
            Assert.AreEqual<int>(expected, actual);

            expected = number << 2;
            actual = jp1 << 2;
            Assert.AreEqual<int>(expected, actual);

            expected = number >> 2;
            actual = jp1 >> 2;
            Assert.AreEqual<int>(expected, actual);
        }
        
        [TestMethod]
        public void JsonObjectOperatorsTest()
        {
            dynamic obj1 = JsonValue.Parse("{\"Name\":\"Hello\",\"Age\":1,\"Grades\":[\"A\",\"B\",\"C\"]}");
            dynamic obj2 = JsonValue.Parse("{\"Name\":\"Hello\",\"Age\":1,\"Grades\":[\"A\",\"B\",\"C\"]}");
            dynamic obj3 = obj1;

            foreach (KeyValuePair<string, JsonValue> pair in obj1)
            {
                dynamic d = pair.Value;

                Assert.IsFalse(d == null);
                Assert.IsTrue(d.IAmJsonDefault == null);
            }

            Assert.IsTrue(obj1 != null);
            Assert.IsTrue(null != obj1);

            Assert.IsTrue(obj1.Name == "Hello");
            Assert.IsTrue(obj1.Age == 1);
            Assert.AreEqual(obj1.Grades.Count, 3);
            Assert.IsTrue(obj1.Grades[0] == "A");
            Assert.IsTrue(obj1.Grades[2] == "C");

            Assert.IsTrue(obj1 == obj3);
            Assert.IsFalse(obj1 == obj2);
        }

        [TestMethod]
        public void JsonArrayOperatorsTests()
        {
            dynamic values1 = JsonValue.Parse("[0,1,2,3,4]");
            dynamic values2 = JsonValue.Parse("[0,1,2,3,4]");
            dynamic values3 = values1;

            Assert.AreEqual(5, values1.Count);
            Assert.IsTrue(values1[0] == 0);
            Assert.IsTrue(values1[1] == 1);
            Assert.IsTrue(values1[2] == 2);
            Assert.IsTrue(values1[3] == 3);
            Assert.IsTrue(values1[4] == 4);

            for (int i = 0; i < values1.Count; i++)
            {
                Assert.IsTrue(values1[i] == i);
            }

            Assert.IsFalse(values1 == values2);
            Assert.IsTrue(values1 == values3);
        }

        [TestMethod]
        public void JsonValueIncompatComparisonTest()
        {
            dynamic ja = AnyInstance.AnyJsonArray;
            dynamic jo = AnyInstance.AnyJsonObject;
            dynamic jp = AnyInstance.AnyJsonPrimitive;
            dynamic jd = AnyInstance.DefaultJsonValue;

            Assert.IsTrue(ja != jo);
            Assert.IsTrue(jo != jp);
            Assert.IsTrue(jp != jd);
            Assert.IsTrue(jd != ja);
            Assert.IsFalse(ja == jo);
            Assert.IsFalse(jo == jp);
            Assert.IsFalse(jp == jd);
            Assert.IsFalse(jd == ja);
        }

        [TestMethod]
        public void JsonValueInvalidOperatorsTest()
        {
            dynamic ja = AnyInstance.AnyJsonArray;
            dynamic jo = AnyInstance.AnyJsonObject;
            dynamic jp = (JsonValue)AnyInstance.AnyString;
            dynamic jd = AnyInstance.DefaultJsonValue;

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja * jd; }, string.Format(OperationNotDefinedMsgFormat, "Multiply", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd / jo; }, string.Format(OperationNotDefinedMsgFormat, "Divide", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo % jp; }, string.Format(OperationNotDefinedMsgFormat, "Modulo", "Object"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja - ja; }, string.Format(OperationNotDefinedMsgFormat, "Subtract", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja + ja; }, string.Format(OperationNotDefinedMsgFormat, "Add", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja > jd; }, string.Format(OperationNotDefinedMsgFormat, "GreaterThan", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd < jo; }, string.Format(OperationNotDefinedMsgFormat, "LessThan", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo >= jp; }, string.Format(OperationNotDefinedMsgFormat, "GreaterThanOrEqual", "Object"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jp <= ja; }, string.Format(OperationNotDefinedMsgFormat, "LessThanOrEqual", "String"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja & ja; }, string.Format(OperationNotDefinedMsgFormat, "And", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd | jd; }, string.Format(OperationNotDefinedMsgFormat, "Or", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo ^ jo; }, string.Format(OperationNotDefinedMsgFormat, "ExclusiveOr", "Object"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jp << ja; }, string.Format(OperationNotDefinedMsgFormat, "LeftShift", "String"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja >> jo; }, string.Format(OperationNotDefinedMsgFormat, "RightShift", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd && true; }, string.Format(OperationNotDefinedMsgFormat, "IsFalse", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo || false; }, string.Format(OperationNotDefinedMsgFormat, "IsTrue", "Object"));

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja * 10; }, string.Format(OperationNotDefinedMsgFormat, "Multiply", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd / 10; }, string.Format(OperationNotDefinedMsgFormat, "Divide", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo % 10; }, string.Format(OperationNotDefinedMsgFormat, "Modulo", "Object"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja + 10; }, string.Format(OperationNotDefinedMsgFormat, "Add", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja - 10; }, string.Format(OperationNotDefinedMsgFormat, "Subtract", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja > 10; }, string.Format(OperationNotDefinedMsgFormat, "GreaterThan", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd >= 10; }, string.Format(OperationNotDefinedMsgFormat, "GreaterThanOrEqual", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo <= 10.5; }, string.Format(OperationNotDefinedMsgFormat, "LessThanOrEqual", "Object"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja & 0x001; }, string.Format(OperationNotDefinedMsgFormat, "And", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd | 0x001; }, string.Format(OperationNotDefinedMsgFormat, "Or", "Default"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo ^ 0x001; }, string.Format(OperationNotDefinedMsgFormat, "ExclusiveOr", "Object"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja << 10; }, string.Format(OperationNotDefinedMsgFormat, "LeftShift", "Array"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja >> 10; }, string.Format(OperationNotDefinedMsgFormat, "RightShift", "Array"));

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jp + "Hello"; }, string.Format(OperatorNotDefinedMsgFormat, "Add", typeof(string).FullName, typeof(string).FullName));

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jp << 10; }, string.Format(OperatorNotDefinedMsgFormat, "LeftShift", "System.String", "System.Int32"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jp <= 10.5; }, string.Format(OperatorNotDefinedMsgFormat, "LessThanOrEqual", "System.String", "System.Double"));

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jd < null; }, string.Format(OperatorNotAllowedOnOperands, "LessThan", typeof(JsonValue), "<null>"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jp > null; }, string.Format(OperatorNotAllowedOnOperands, "GreaterThan", typeof(JsonPrimitive), "<null>"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = ja + null; }, string.Format(OperatorNotAllowedOnOperands, "Add", typeof(JsonArray), "<null>"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = jo & null; }, string.Format(OperatorNotAllowedOnOperands, "And", typeof(JsonObject), "<null>"));


            dynamic dyn = (JsonValue)20;
            string jpTypeName = typeof(JsonPrimitive).FullName;

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = ++dyn; }, string.Format(OperationNotDefinedMsgFormat, "Increment", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = --dyn; }, string.Format(OperationNotDefinedMsgFormat, "Decrement", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn++; }, string.Format(OperationNotDefinedMsgFormat, "Increment", "Number"));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = dyn--; }, string.Format(OperationNotDefinedMsgFormat, "Decrement", "Number"));
        }


        [TestMethod]
        public void JsonValueInvalidOperationsTest()
        {
            dynamic boolDyn = (JsonValue)AnyInstance.AnyBool;
            dynamic strDyn = (JsonValue)AnyInstance.AnyString;
            dynamic intDyn = (JsonValue)AnyInstance.AnyInt;
            dynamic charDyn = (JsonValue)AnyInstance.AnyChar;

            Assert.IsTrue(intDyn == AnyInstance.AnyInt);
            Assert.IsTrue(charDyn == AnyInstance.AnyChar);
            Assert.IsTrue(boolDyn == AnyInstance.AnyBool);
            Assert.IsTrue(strDyn == AnyInstance.AnyString);

            ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = AnyInstance.AnyBool == boolDyn; });
            ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = AnyInstance.AnyString == strDyn; });
            ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = AnyInstance.AnyInt == intDyn; });
            ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = AnyInstance.AnyChar == charDyn; });
            ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = true > boolDyn; });
            ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = 10 > boolDyn; });

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = ~boolDyn; });
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = boolDyn > 8; });
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = boolDyn > true; });
        }

        [TestMethod]
        public void JsonPrimitiveInvalidOperatorsTest()
        {
            dynamic dyn1 = (JsonPrimitive)3;
            dynamic dyn2 = (JsonPrimitive)"Hello";

            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 == dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "Equal", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 != dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "NotEqual", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn2 > dyn1; }, string.Format(OperatorNotDefinedMsgFormat, "GreaterThan", typeof(string).FullName, typeof(int).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn2 < dyn1; }, string.Format(OperatorNotDefinedMsgFormat, "LessThan", typeof(string).FullName, typeof(int).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 >= dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "GreaterThanOrEqual", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 <= dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "LessThanOrEqual", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn2 * dyn1; }, string.Format(OperatorNotDefinedMsgFormat, "Multiply", typeof(string).FullName, typeof(int).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn2 / dyn1; }, string.Format(OperatorNotDefinedMsgFormat, "Divide", typeof(string).FullName, typeof(int).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 % dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "Modulo", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 << dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "LeftShift", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn2 >> dyn1; }, string.Format(OperatorNotDefinedMsgFormat, "RightShift", typeof(string).FullName, typeof(int).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn2 & dyn1; }, string.Format(OperatorNotDefinedMsgFormat, "And", typeof(string).FullName, typeof(int).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 | dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "Or", typeof(int).FullName, typeof(string).FullName));
            ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var val = dyn1 ^ dyn2; }, string.Format(OperatorNotDefinedMsgFormat, "ExclusiveOr", typeof(int).FullName, typeof(string).FullName));
        }

        [TestMethod]
        public void JsonCollectionsInvalidOperationsTest()
        {
            dynamic[] values = { AnyInstance.AnyJsonArray, AnyInstance.AnyJsonObject };

            for (int i = 0; i < values.Length; i++)
            {
                dynamic jv = values[i];

                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv++; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv--; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv > 0; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv >= 0; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv < 0; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv <= 0; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv + 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv * 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv / 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv << 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv || 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv && 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv ^ 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv & 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv | 1; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = ~jv; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = !jv; });

                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv == "Hello"; });
                ExceptionTestHelper.ExpectException<InvalidOperationException>(delegate { var v = jv != 0; });

                ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = 0 != jv; });
                ExceptionTestHelper.ExpectException<RuntimeBinderException>(delegate { var v = 0 == jv; });
            }
        }
    }
}
