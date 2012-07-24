namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class Json
    {
        public static string Encode(object obj)
        {
            return JsonValueExtensions.CreateFrom(obj).ToString();
        }

        public static dynamic Decode<T>(string json)
        {
            return JsonValue.Parse(json).ReadAsType<T>();
        }

        public static dynamic Decode(string json)
        {
            return JsonValue.Parse(json);
        }
    }
    
    [TestClass]
    public class WebMatrixCompatTest
    {
        [TestMethod]
        public void EncodeWithDynamicObject()
        {
            // Arrange
            dynamic obj = new DummyDynamicObject();
            obj.Name = "Hello";
            obj.Age = 1;
            obj.Grades = new[] { "A", "B", "C" };

            // Act
            string json = Json.Encode(obj);

            // Assert
            Assert.AreEqual("{\"Name\":\"Hello\",\"Age\":1,\"Grades\":[\"A\",\"B\",\"C\"]}", json);
        }

        [TestMethod]
        public void EncodeArray()
        {
            // Arrange
            object input = new string[] { "one", "2", "three", "4" };

            // Act
            string json = Json.Encode(input);

            // Assert
            Assert.AreEqual("[\"one\",\"2\",\"three\",\"4\"]", json);
        }

        [TestMethod]
        public void EncodeDynamicJsonArrayEncodesAsArray()
        {
            // Arrange
            dynamic array = Json.Decode("[1,2,3]");

            // Act
            string json = Json.Encode(array);

            // Assert
            Assert.AreEqual("[1,2,3]", json);
        }

        [TestMethod]
        public void DecodeDynamicObject()
        {
            // Act
            var obj = Json.Decode("{\"Name\":\"Hello\",\"Age\":1,\"Grades\":[\"A\",\"B\",\"C\"]}");

            // Note: binary operators on dynamic JsonValue are not symmetric.
            Assert.IsTrue(obj.Name == "Hello");
            Assert.IsTrue(obj.Age == 1);
            Assert.AreEqual(obj.Grades.Count, 3);
            Assert.IsTrue(obj.Grades[0] == "A");
            Assert.IsTrue(obj.Grades[1] == "B");
            Assert.IsTrue(obj.Grades[2] == "C");

            // Note: Assert.AreEqual checks object types, not only values and the generic version is not supported by the DLR (cast not honored).
            // Assert
            /*
            Assert.AreEqual("Hello", obj.Name);
            Assert.AreEqual(1, obj.Age);
            Assert.AreEqual(3, obj.Grades.Length);
            Assert.AreEqual("A", obj.Grades[0]);
            Assert.AreEqual("B", obj.Grades[1]);
            Assert.AreEqual("C", obj.Grades[2]);
            */
        }

        [TestMethod]
        public void DecodeDynamicObjectImplicitConversionToDictionary()
        {
            // Act
            /*
            IDictionary<string, object> values = Json.Decode("{\"Name\":\"Hello\",\"Age\":1}");
            */
            IDictionary<string, object> values = Json.Decode("{\"Name\":\"Hello\",\"Age\":1}").ToDictionary();

            // Assert
            Assert.AreEqual("Hello", values["Name"]);
            Assert.AreEqual(1, values["Age"]);
        }

        [TestMethod]
        public void DecodeArrayImplicitConversionToArrayAndObjectArray()
        {
            // Act
            /*
            Array array = Json.Decode("[1,2,3]");
            object[] objArray = Json.Decode("[1,2,3]");
            */
            Array array = Json.Decode("[1,2,3]").ToObjectArray();
            object[] objArray = Json.Decode("[1,2,3]").ToObjectArray();

            // Note: this string notation is not valid JSON so JsonValue does not support it.
            /*
            IEnumerable<dynamic> dynamicEnumerable = Json.Decode("[{a:1}]");
            */
            IEnumerable<dynamic> dynamicEnumerable = Json.Decode("[{\"a\":1}]") as dynamic;

            // Assert
            Assert.IsNotNull(array);
            Assert.IsNotNull(objArray);
            Assert.IsNotNull(dynamicEnumerable);
        }

        [TestMethod]
        public void DecodeArrayImplicitConversionToArrayArrayValuesAreDynamic()
        {
            // Act            
            // Note: The WebMatrix cast is not deep, here you have a object[] of DynamicJsonObject types.
            /*
            dynamic[] objArray = Json.Decode("[{\"A\":1}]");
            */
            dynamic objArray = Json.Decode("[{\"A\":1}]");

            // Assert
            Assert.IsNotNull(objArray);
            Assert.IsTrue(objArray[0].A == 1);

            /*
            Assert.AreEqual(1, objArray[0].A);
            */
        }

        [TestMethod]
        public void DecodeDynamicObjectAccessPropertiesByIndexer()
        {
            // Arrange
            var obj = Json.Decode("{\"Name\":\"Hello\",\"Age\":1,\"Grades\":[\"A\",\"B\",\"C\"]}");

            // Assert
            Assert.IsTrue(obj["Name"] == "Hello");
            Assert.IsTrue(obj["Age"] == 1);
            Assert.AreEqual(3, obj["Grades"].Count);
            Assert.IsTrue(obj["Grades"][0] == "A");
            Assert.IsTrue(obj["Grades"][1] == "B");
            Assert.IsTrue(obj["Grades"][2] == "C");

            obj = obj.ToDictionary();

            Assert.AreEqual("Hello", obj["Name"]);
            Assert.AreEqual(1, obj["Age"]);
            Assert.AreEqual(3, obj["Grades"].Length);
            Assert.AreEqual("A", obj["Grades"][0]);
            Assert.AreEqual("B", obj["Grades"][1]);
            Assert.AreEqual("C", obj["Grades"][2]);
        }

        [TestMethod]
        public void DecodeDynamicObjectAccessPropertiesByNullIndexerReturnsNull()
        {
            // Arrange
            var obj = Json.Decode("{\"Name\":\"Hello\",\"Age\":1,\"Grades\":[\"A\",\"B\",\"C\"]}");

            // Assert
            /*
            Assert.IsNull(obj[null]);
            */
            Assert.IsTrue(obj.@null == null);
        }

        [TestMethod]
        public void DecodeDateTime()
        {
            // Act
            /*
            DateTime dateTime = Json.Decode("\"\\/Date(940402800000)\\/\"");
            */
            DateTime dateTime = (DateTime)Json.Decode("\"\\/Date(940402800000)\\/\"");

            // Assert
            Assert.AreEqual(1999, dateTime.Year);
            Assert.AreEqual(10, dateTime.Month);
            Assert.AreEqual(20, dateTime.Day);
        }

        [TestMethod]
        public void DecodeNumber()
        {
            // Act
            int number = Json.Decode("1");

            // Assert
            Assert.AreEqual(1, number);
        }

        [TestMethod]
        public void DecodeString()
        {
            // Act
            string @string = Json.Decode("\"1\"");

            // Assert
            Assert.AreEqual("1", @string);
        }

        [TestMethod]
        public void DecodeArray()
        {
            // Act
            var values = Json.Decode("[11,12,13,14,15]");
      
            Assert.AreEqual(5, values.Count);
            Assert.IsTrue(values[0] == 11);
            Assert.IsTrue(values[1] == 12);
            Assert.IsTrue(values[2] == 13);
            Assert.IsTrue(values[3] == 14);
            Assert.IsTrue(values[4] == 15);

            values = values.ToObjectArray();

            // Assert            
            Assert.AreEqual(5, values.Length);
            Assert.AreEqual(11, values[0]);
            Assert.AreEqual(12, values[1]);
            Assert.AreEqual(13, values[2]);
            Assert.AreEqual(14, values[3]);
            Assert.AreEqual(15, values[4]);
        }

        [TestMethod]
        public void DecodeObjectWithArrayProperty()
        {
            // Act
            var obj = Json.Decode("{\"A\":1,\"B\":[1,3,4]}");

            /*
            object[] bValues = obj.B;
            */
            object[] bValues = obj.B.ToObjectArray();

            // Assert
            /*
            Assert.AreEqual(1, obj.A);
            */
            Assert.AreEqual(1, (int)obj.A);
            Assert.AreEqual(1, bValues[0]);
            Assert.AreEqual(3, bValues[1]);
            Assert.AreEqual(4, bValues[2]);
        }

        [TestMethod]
        public void DecodeArrayWithObjectValues()
        {
            // Act
            var obj = Json.Decode("[{\"A\":1},{\"B\":3, \"C\": \"hello\"}]");

            Assert.AreEqual(2, obj.Count);
            Assert.IsTrue(obj[0].A == 1);
            Assert.IsTrue(obj[1].B == 3);
            Assert.IsTrue(obj[1].C == "hello");

            // Assert
            /*
            Assert.AreEqual(2, obj.Length);
            Assert.AreEqual(1, obj[0].A);
            Assert.AreEqual(3, obj[1].B);
            Assert.AreEqual("hello", obj[1].C);
            */
        }

        [TestMethod]
        public void DecodeArraySetValues()
        {
            // Arrange
            var values = Json.Decode("[1,2,3,4,5]");

            // Note: JsonPrimitive is immutable
            for (int i = 0; i < values.Count; i++)
            {
                /*
                values[i]++;
                */
                values[i] = values[i] + 1;
            }

            // Assert
            Assert.AreEqual(5, values.Count);
            Assert.AreEqual(2, (int)values[0]);
            Assert.AreEqual(3, (int)values[1]);
            Assert.AreEqual(4, (int)values[2]);
            Assert.AreEqual(5, (int)values[3]);
            Assert.AreEqual(6, (int)values[4]);

            /*
            Assert.AreEqual(5, values.Length);
            Assert.AreEqual(2, values[0]);
            Assert.AreEqual(3, values[1]);
            Assert.AreEqual(4, values[2]);
            Assert.AreEqual(5, values[3]);
            Assert.AreEqual(6, values[4]);
            */
        }

        [TestMethod]
        public void DecodeArrayPassToMethodThatTakesArray()
        {
            // Arrange
            /*
            var values = Json.Decode("[3,2,1]");
            */
            var values = Json.Decode("[3,2,1]").ToObjectArray();

            // Act
            int index = Array.IndexOf(values, 2);

            // Assert
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void DecodeArrayGetEnumerator()
        {
            // Arrange
            var values = Json.Decode("[1,2,3]");

            // Assert
            int val = 1;
            foreach (var value in values)
            {
                /*
                Assert.AreEqual(val, value);
                */
                Assert.IsTrue(value == val);
                val++;
            }
        }

        [TestMethod]
        public void DecodeObjectPropertyAccessIsSameObjectInstance()
        {
            // Arrange
            var obj = Json.Decode("{\"Name\":{\"Version:\":4.0, \"Key\":\"Key\"}}");

            // Assert
            Assert.AreSame(obj.Name, obj.Name);
        }

        [TestMethod]
        public void DecodeArrayAccessingMembersThatDontExistReturnsNull()
        {
            // Act
            var obj = Json.Decode("[\"a\", \"b\"]");

            // Assert
            /*
            Assert.IsNull(obj.PropertyThatDoesNotExist);
            */
            Assert.IsTrue(obj.PropertyThatDoesNotExist == null);
        }

        [TestMethod]
        public void DecodeObjectSetProperties()
        {
            // Act
            var obj = Json.Decode("{\"A\":{\"B\":100}}");

            obj.A.B = 20;

            // Assert
            /*
            Assert.AreEqual(20, obj.A.B);
            */
            Assert.IsTrue(obj.A.B == 20);
        }

        //BUG#195159 - Cannot serialize anonymous types.
        //[TestMethod]
        public void DecodeObjectSettingObjectProperties()
        {
            // Act
            var obj = Json.Decode("{\"A\":1}");

            obj.A = new { B = 1, D = 2 };

            // Assert
            Assert.AreEqual(1, obj.A.B);
            Assert.AreEqual(2, obj.A.D);
        }

        [TestMethod]
        public void DecodeObjectWithArrayPropertyPassPropertyToMethodThatTakesArray()
        {
            // Arrange
            var obj = Json.Decode("{\"A\":[3,2,1]}");

            object[] arr = obj.A.ToObjectArray();

            // Act
            /*
            Array.Sort(obj.A);
            */
            Array.Sort(arr);

            // Assert
            /*
            Assert.AreEqual(1, obj.A[0]);
            Assert.AreEqual(2, obj.A[1]);
            Assert.AreEqual(3, obj.A[2]);
            */
            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(2, arr[1]);
            Assert.AreEqual(3, arr[2]);
        }

        [TestMethod]
        public void DecodeObjectAccessingMembersThatDontExistReturnsNull()
        {
            // Act
            var obj = Json.Decode("{\"A\":1}");

            // Assert
            /*
            Assert.IsNull(obj.PropertyThatDoesntExist);
            */
            Assert.IsTrue(obj.PropertyThatDoesntExist == null);
        }

        [TestMethod]
        public void DecodeObjectWithSpecificType()
        {
            // Act
            var person = Json.Decode<Person>("{\"Name\":\"David\", \"Age\":2}");

            // Assert
            Assert.AreEqual("David", person.Name);
            Assert.AreEqual(2, person.Age);
        }

        [TestMethod]
        public void DecodeObjectWithImplicitConversionToNonDynamicTypeThrows()
        {
            // Act & Assert
            /*
            ExceptionAssert.Throws<InvalidOperationException>(() => 
            {
                Person person = Json.Decode("{\"Name\":\"David\", \"Age\":2, \"Address\":{\"Street\":\"Bellevue\"}}");
            },"Unable to convert to \"System.Web.Helpers.Test.JsonTest+Person\". Use Json.Decode<T> instead.");
            */

            ExceptionTestHelper.ExpectException<InvalidCastException>(() =>
            {
                Person person = Json.Decode("{\"Name\":\"David\", \"Age\":2, \"Address\":{\"Street\":\"Bellevue\"}}");
            });
        }

        private class DummyDynamicObject : DynamicObject
        {
            private IDictionary<string, object> _values = new Dictionary<string, object>();

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return _values.Keys;
            }

            public override bool TrySetMember(SetMemberBinder binder, object value)
            {
                _values[binder.Name] = value;
                return true;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                return _values.TryGetValue(binder.Name, out result);
            }
        }
    }
}
