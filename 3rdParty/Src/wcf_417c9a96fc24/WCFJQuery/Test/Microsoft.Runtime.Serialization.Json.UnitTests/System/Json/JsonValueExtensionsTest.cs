namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonValueExtesnsionsTest
    {
        const string DynamicPropertyNotDefined = "'{0}' does not contain a definition for property '{1}'.";
        const string OperationNotSupportedOnJsonTypeMsgFormat = "Operation not supported on JsonValue instance of 'JsonType.{0}' type.";


        [TestMethod()]
        public void CreateFromTypeTest()
        {
            JsonValue[] values =
            {
                AnyInstance.AnyJsonObject,
                AnyInstance.AnyJsonArray,
                AnyInstance.AnyJsonPrimitive,
                AnyInstance.DefaultJsonValue
            };

            foreach (JsonValue value in values)
            {
                Assert.AreSame(value, JsonValueExtensions.CreateFrom(value));
            }
        }

        [TestMethod()]
        public void CreateFromPrimitiveTest()
        {
            object[] values = 
            {
                AnyInstance.AnyBool,
                AnyInstance.AnyByte,
                AnyInstance.AnyChar,
                AnyInstance.AnyDateTime,
                AnyInstance.AnyDateTimeOffset,
                AnyInstance.AnyDecimal,
                AnyInstance.AnyDouble,
                AnyInstance.AnyFloat,
                AnyInstance.AnyGuid,
                AnyInstance.AnyLong,
                AnyInstance.AnySByte,
                AnyInstance.AnyShort,
                AnyInstance.AnyUInt,
                AnyInstance.AnyULong,
                AnyInstance.AnyUri,
                AnyInstance.AnyUShort,
                AnyInstance.AnyInt, 
                AnyInstance.AnyString, 

            };

            foreach (object value in values)
            {
                Type valueType = value.GetType();
                Console.WriteLine("Value: {0}, Type: {1}", value, valueType);
                Assert.AreEqual(value, JsonValueExtensions.CreateFrom(value).ReadAs(valueType), "Test failed on value of type: " + valueType);
            }
        }

        [TestMethod()]
        public void CreateFromComplexTest()
        {
            JsonValue target = JsonValueExtensions.CreateFrom(AnyInstance.AnyPerson);

            Assert.AreEqual(AnyInstance.AnyPerson.Name, (string)target["Name"]);
            Assert.AreEqual(AnyInstance.AnyPerson.Age, (int)target["Age"]);
            Assert.AreEqual(AnyInstance.AnyPerson.Address.City, (string)target.ValueOrDefault("Address", "City"));
        }

        [TestMethod]
        public void CreateFromDynamicSimpleTest()
        {
            JsonValue target;

            target = JsonValueExtensions.CreateFrom(AnyInstance.AnyDynamic);
            Assert.IsNotNull(target);

            string expected = "{\"Name\":\"Bill Gates\",\"Age\":21,\"Grades\":[\"F\",\"B-\",\"C\"]}";
            dynamic obj = new TestDynamicObject();
            obj.Name = "Bill Gates";
            obj.Age = 21;
            obj.Grades = new[] { "F", "B-", "C" };

            target = JsonValueExtensions.CreateFrom(obj);
            Assert.AreEqual<string>(expected, target.ToString());

            target = JsonValueExtensions.CreateFrom(new TestDynamicObject());
            Assert.AreEqual<string>("{}", target.ToString());
        }

        [TestMethod]
        public void CreateFromDynamicComplextTest()
        {
            JsonValue target;
            Person person = AnyInstance.AnyPerson;
            dynamic dyn = TestDynamicObject.CreatePersonAsDynamic(person);

            dyn.TestProperty = AnyInstance.AnyString;

            target = JsonValueExtensions.CreateFrom(dyn);
            Assert.IsNotNull(target);
            Assert.AreEqual<string>(AnyInstance.AnyString, dyn.TestProperty);
            Person jvPerson = target.ReadAsType<Person>();
            Assert.AreEqual(person.ToString(), jvPerson.ToString());

            Person p1 = Person.CreateSample();
            Person p2 = Person.CreateSample();

            p2.Name += "__2";
            p2.Age += 10;
            p2.Address.City += "__2";

            Person[] friends = new Person[] { p1, p2 };
            target = JsonValueExtensions.CreateFrom(friends);
            Person[] personArr = target.ReadAsType<Person[]>();
            Assert.AreEqual<int>(friends.Length, personArr.Length);
            Assert.AreEqual<string>(friends[0].ToString(), personArr[0].ToString());
            Assert.AreEqual<string>(friends[1].ToString(), personArr[1].ToString());
        }

        [TestMethod]
        public void CreateFromDynamicBinderFallbackTest()
        {
            JsonValue target;
            Person person = AnyInstance.AnyPerson;
            dynamic dyn = new TestDynamicObject();
            dyn.Name = AnyInstance.AnyString;
  
            dyn.UseFallbackMethod = true;
            string expectedMessage = string.Format(DynamicPropertyNotDefined, dyn.GetType().FullName, "Name");
            ExceptionTestHelper.ExpectException<InvalidOperationException>(() => target = JsonValueExtensions.CreateFrom(dyn), expectedMessage);

            dyn.UseErrorSuggestion = true;
            ExceptionTestHelper.ExpectException<TestDynamicObject.TestDynamicObjectException>(() => target = JsonValueExtensions.CreateFrom(dyn));
        }

        [TestMethod]
        public void CreateFromNestedDynamicTest()
        {
            JsonValue target;
            string expected = "{\"Name\":\"Root\",\"Level1\":{\"Name\":\"Level1\",\"Level2\":{\"Name\":\"Level2\"}}}";
            dynamic dyn = new TestDynamicObject();
            dyn.Name = "Root";
            dyn.Level1 = new TestDynamicObject();
            dyn.Level1.Name = "Level1";
            dyn.Level1.Level2 = new TestDynamicObject();
            dyn.Level1.Level2.Name = "Level2";

            target = JsonValueExtensions.CreateFrom(dyn);
            Assert.IsNotNull(target);
            Assert.AreEqual<string>(expected, target.ToString());
        }

        [TestMethod]
        public void CreateFromDynamicWithJsonValueChildrenTest()
        {
            JsonValue target;
            string level3 = "{\"Name\":\"Level3\",\"Null\":null}";
            string level2 = "{\"Name\":\"Level2\",\"JsonObject\":" + AnyInstance.AnyJsonObject + ",\"JsonArray\":" + AnyInstance.AnyJsonArray + ",\"Level3\":" + level3 + "}";
            string level1 = "{\"Name\":\"Level1\",\"JsonPrimitive\":" + AnyInstance.AnyJsonPrimitive + ",\"Level2\":" + level2 + "}";
            string expected = "{\"Name\":\"Root\",\"Level1\":" + level1 + "}";

            dynamic dyn = new TestDynamicObject();
            dyn.Name = "Root";
            dyn.Level1 = new TestDynamicObject();
            dyn.Level1.Name = "Level1";
            dyn.Level1.JsonPrimitive = AnyInstance.AnyJsonPrimitive;
            dyn.Level1.Level2 = new TestDynamicObject();
            dyn.Level1.Level2.Name = "Level2";
            dyn.Level1.Level2.JsonObject = AnyInstance.AnyJsonObject;
            dyn.Level1.Level2.JsonArray = AnyInstance.AnyJsonArray;
            dyn.Level1.Level2.Level3 = new TestDynamicObject();
            dyn.Level1.Level2.Level3.Name = "Level3";
            dyn.Level1.Level2.Level3.Null = null;

            target = JsonValueExtensions.CreateFrom(dyn);
            Assert.AreEqual<string>(expected, target.ToString());
        }

        [TestMethod]
        public void CreateFromDynamicJVTest()
        {
            JsonValue target;

            dynamic[] values = new dynamic[]
            {
                AnyInstance.AnyJsonArray,
                AnyInstance.AnyJsonObject,
                AnyInstance.AnyJsonPrimitive,
                AnyInstance.DefaultJsonValue
            };

            foreach (dynamic dyn in values)
            {
                target = JsonValueExtensions.CreateFrom(dyn);
                Assert.AreSame(dyn, target);
            }
        }

        [TestMethod]
        public void ReadAsTypeFallbackTest()
        {
            JsonValue jv = AnyInstance.AnyInt;
            Person personFallback = Person.CreateSample();

            Person personResult = jv.ReadAsType<Person>(personFallback);
            Assert.AreSame(personFallback, personResult);

            int intFallback = 45;
            int intValue = jv.ReadAsType<int>(intFallback);
            Assert.AreEqual<int>(AnyInstance.AnyInt, intValue);
        }

        [TestMethod]
        public void ReadAsTypeCollectionTest()
        {
            JsonValue jsonValue;
            jsonValue = JsonValue.Parse("[1,2,3]");

            List<object> list = jsonValue.ReadAsType<List<object>>();
            Array array = jsonValue.ReadAsType<Array>();
            object[] objArr = jsonValue.ReadAsType<object[]>();

            IList[] collections = 
            {
                list, array, objArr
            };

            foreach (IList collection in collections)
            {
                Assert.AreEqual<int>(jsonValue.Count, collection.Count);

                for (int i = 0; i < jsonValue.Count; i++)
                {
                    Assert.AreEqual<int>((int)jsonValue[i], (int)collection[i]);
                }
            }

            jsonValue = JsonValue.Parse("{\"A\":1,\"B\":2,\"C\":3}");
            Dictionary<string, object> dictionary = jsonValue.ReadAsType<Dictionary<string, object>>();

            Assert.AreEqual<int>(jsonValue.Count, dictionary.Count);
            foreach (KeyValuePair<string, JsonValue> pair in jsonValue)
            {
                Assert.AreEqual((int)jsonValue[pair.Key], (int)dictionary[pair.Key]);
            }
        }

        [TestMethod]
        public void TryReadAsInvalidCollectionTest()
        {
            JsonValue jo = AnyInstance.AnyJsonObject;
            JsonValue ja = AnyInstance.AnyJsonArray;
            JsonValue jp = AnyInstance.AnyJsonPrimitive;
            JsonValue jd = AnyInstance.DefaultJsonValue;

            JsonValue[] invalidArrays = 
            {
                jo, jp, jd
            };

            JsonValue[] invalidDictionaries =
            {
                ja, jp, jd
            };

            bool success;
            object[] array;
            Dictionary<string, object> dictionary;

            foreach (JsonValue value in invalidArrays)
            {
                success = value.TryReadAsType<object[]>(out array);
                Console.WriteLine("Try reading {0} as object[]; success = {1}", value.ToString(), success);
                Assert.IsFalse(success);
                Assert.IsNull(array);
            }

            foreach (JsonValue value in invalidDictionaries)
            {
                success = value.TryReadAsType<Dictionary<string, object>>(out dictionary);
                Console.WriteLine("Try reading {0} as Dictionary<string, object>; success = {1}", value.ToString(), success);
                Assert.IsFalse(success);
                Assert.IsNull(dictionary);
            }
        }

        [TestMethod]
        public void SaveExtensionsOnDynamicTest()
        {
            string json = "{\"a\":123,\"b\":[false,null,12.34]}";
            string expectedJxml = "<root type=\"object\"><a type=\"number\">123</a><b type=\"array\"><item type=\"boolean\">false</item><item type=\"null\"/><item type=\"number\">12.34</item></b></root>";
            dynamic target = JsonValue.Parse(json);
            using (MemoryStream ms = new MemoryStream())
            {
                using (XmlDictionaryWriter xdw = XmlDictionaryWriter.CreateTextWriter(ms))
                {
                    target.Save(xdw);
                    xdw.Flush();
                    string saved = Encoding.UTF8.GetString(ms.ToArray());
                    Assert.AreEqual(expectedJxml, saved);
                }
            }
        }

        [TestMethod]
        public void ReadAsExtensionsOnDynamicTest()
        {
            dynamic jv = JsonValueExtensions.CreateFrom(AnyInstance.AnyPerson);
            bool success;
            object obj;

            success = jv.TryReadAsType(typeof(Person), out obj);
            Assert.IsTrue(success);
            Assert.IsNotNull(obj);
            Assert.AreEqual<string>(AnyInstance.AnyPerson.ToString(), obj.ToString());

            obj = jv.ReadAsType(typeof(Person));
            Assert.IsNotNull(obj);
            Assert.AreEqual<string>(AnyInstance.AnyPerson.ToString(), obj.ToString());
        }

        [TestMethod]
        public void ToCollectionTest()
        {
            JsonValue target;
            object[] array;

            target = AnyInstance.AnyJsonArray;
            array = target.ToObjectArray();

            Assert.AreEqual(target.Count, array.Length);

            for (int i = 0; i < target.Count; i++)
            {
                Assert.AreEqual(array[i], target[i].ReadAs(array[i].GetType()));
            }

            target = AnyInstance.AnyJsonObject;
            IDictionary<string, object> dictionary = target.ToDictionary();

            Assert.AreEqual(target.Count, dictionary.Count);

            foreach (KeyValuePair<string, JsonValue> pair in target)
            {
                Assert.IsTrue(dictionary.ContainsKey(pair.Key));
                Assert.AreEqual<string>(target[pair.Key].ToString(), dictionary[pair.Key].ToString());
            }
        }

        [TestMethod]
        public void ToCollectionsNestedTest()
        {
            JsonArray ja = JsonValue.Parse("[1, {\"A\":[1,2,3]}, 5]") as JsonArray;
            JsonObject jo = JsonValue.Parse("{\"A\":1,\"B\":[1,2,3]}") as JsonObject;

            object[] objArray = ja.ToObjectArray();
            Assert.IsNotNull(objArray);
            Assert.AreEqual<int>(ja.Count, objArray.Length);
            Assert.AreEqual((int)ja[0], (int)objArray[0]);
            Assert.AreEqual((int)ja[2], (int)objArray[2]);

            IDictionary<string, object> dict = objArray[1] as IDictionary<string, object>;
            Assert.IsNotNull(dict);

            objArray = dict["A"] as object[];
            Assert.IsNotNull(objArray);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(i + 1, (int)objArray[i]);
            }

            dict = jo.ToDictionary();
            Assert.IsNotNull(dict);
            Assert.AreEqual<int>(jo.Count, dict.Count);
            Assert.AreEqual<int>(1, (int)dict["A"]);

            objArray = dict["B"] as object[];
            Assert.IsNotNull(objArray);
            for (int i = 1; i < 3; i++)
            {
                Assert.AreEqual(i + 1, (int)objArray[i]);
            }
        }

        [TestMethod]
        public void ToCollectionsInvalidTest()
        {
            JsonValue jo = AnyInstance.AnyJsonObject;
            JsonValue ja = AnyInstance.AnyJsonArray;
            JsonValue jp = AnyInstance.AnyJsonPrimitive;
            JsonValue jd = AnyInstance.DefaultJsonValue;

            ExceptionTestHelper.ExpectException<NotSupportedException>(delegate { var ret = jd.ToObjectArray(); }, string.Format(OperationNotSupportedOnJsonTypeMsgFormat, jd.JsonType));
            ExceptionTestHelper.ExpectException<NotSupportedException>(delegate { var ret = jd.ToDictionary(); }, string.Format(OperationNotSupportedOnJsonTypeMsgFormat, jd.JsonType));

            ExceptionTestHelper.ExpectException<NotSupportedException>(delegate { var ret = jp.ToObjectArray(); }, string.Format(OperationNotSupportedOnJsonTypeMsgFormat, jp.JsonType));
            ExceptionTestHelper.ExpectException<NotSupportedException>(delegate { var ret = jp.ToDictionary(); }, string.Format(OperationNotSupportedOnJsonTypeMsgFormat, jp.JsonType));

            ExceptionTestHelper.ExpectException<NotSupportedException>(delegate { var ret = jo.ToObjectArray(); }, string.Format(OperationNotSupportedOnJsonTypeMsgFormat, jo.JsonType));
            ExceptionTestHelper.ExpectException<NotSupportedException>(delegate { var ret = ja.ToDictionary(); }, string.Format(OperationNotSupportedOnJsonTypeMsgFormat, ja.JsonType));
        }

        // 195843 JsonValue to support generic extension methods defined in JsonValueExtensions.
        // 195867 Consider creating extension point for allowing new extension methods to be callable via dynamic interface
        //[TestMethod] This requires knowledge of the C# binder to be able to get the generic call parameters.
        public void ReadAsGenericExtensionsOnDynamicTest()
        {
            dynamic jv = JsonValueExtensions.CreateFrom(AnyInstance.AnyPerson);
            Person person;
            bool success;

            person = jv.ReadAsType<Person>();
            Assert.IsNotNull(person);
            Assert.AreEqual<string>(AnyInstance.AnyPerson.ToString(), person.ToString());

            success = jv.TryReadAsType<Person>(out person);
            Assert.IsTrue(success);
            Assert.IsNotNull(person);
            Assert.AreEqual<string>(AnyInstance.AnyPerson.ToString(), person.ToString());
        }
    }
}
