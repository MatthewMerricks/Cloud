namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Json;
    using System.Runtime.Serialization.Json;
    using Microsoft.CSharp.RuntimeBinder;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class JsonObjectTest
    {
        [TestMethod]
        public void JsonObjectConstructorEnumTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            List<KeyValuePair<string, JsonValue>> items = new List<KeyValuePair<string, JsonValue>>()
            {
                new KeyValuePair<string, JsonValue>(key1, value1),
                new KeyValuePair<string, JsonValue>(key2, value2),
            };

            JsonObject target = new JsonObject(null);
            Assert.AreEqual(0, target.Count);
            
            target = new JsonObject(items);
            Assert.AreEqual(2, target.Count);
            ValidateJsonObjectItems(target, key1, value1, key2, value2);

            // Invalid tests
            items.Add(new KeyValuePair<string, JsonValue>(key1, AnyInstance.DefaultJsonValue));
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject(items); });
        }

        [TestMethod]
        public void JsonObjectConstructorParmsTest()
        {
            JsonObject target = new JsonObject();
            Assert.AreEqual(0, target.Count);

            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            List<KeyValuePair<string, JsonValue>> items = new List<KeyValuePair<string, JsonValue>>()
            {
                new KeyValuePair<string, JsonValue>(key1, value1),
                new KeyValuePair<string, JsonValue>(key2, value2),
            };

            target = new JsonObject(items[0], items[1]);
            Assert.AreEqual(2, target.Count);
            ValidateJsonObjectItems(target, key1, value1, key2, value2);

            target = new JsonObject(items.ToArray());
            Assert.AreEqual(2, target.Count);
            ValidateJsonObjectItems(target, key1, value1, key2, value2);

            // Invalid tests
            items.Add(new KeyValuePair<string, JsonValue>(key1, AnyInstance.DefaultJsonValue));
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject(items[0], items[1], items[2]); });
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject(items.ToArray()); });
        }

        [TestMethod]
        public void AddTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target;
            
            target = new JsonObject();
            target.Add(new KeyValuePair<string, JsonValue>(key1, value1));
            Assert.AreEqual(1, target.Count);
            Assert.IsTrue(target.ContainsKey(key1));
            Assert.AreEqual(value1, target[key1]);

            target.Add(key2, value2);
            Assert.AreEqual(2, target.Count);
            Assert.IsTrue(target.ContainsKey(key2));
            Assert.AreEqual(value2, target[key2]);

            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { new JsonObject().Add(null, value1); });
            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { new JsonObject().Add(new KeyValuePair<string, JsonValue>(null, value1)); });

            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject().Add(key1, AnyInstance.DefaultJsonValue); });
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonArray().Add(AnyInstance.DefaultJsonValue); });
        }

        [TestMethod]
        public void AddRangeParamsTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            List<KeyValuePair<string, JsonValue>> items = new List<KeyValuePair<string, JsonValue>>()
            {
                new KeyValuePair<string, JsonValue>(key1, value1),
                new KeyValuePair<string, JsonValue>(key2, value2),
            };

            JsonObject target;
            
            target = new JsonObject();
            target.AddRange(items[0], items[1]);
            Assert.AreEqual(2, target.Count);
            ValidateJsonObjectItems(target, key1, value1, key2, value2);

            target = new JsonObject();
            target.AddRange(items.ToArray());
            Assert.AreEqual(2, target.Count);
            ValidateJsonObjectItems(target, key1, value1, key2, value2);

            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { new JsonObject().AddRange((KeyValuePair<string, JsonValue>[])null); });
            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { new JsonObject().AddRange((IEnumerable<KeyValuePair<string, JsonValue>>)null); });

            items[1] = new KeyValuePair<string, JsonValue>(key2, AnyInstance.DefaultJsonValue);
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject().AddRange(items.ToArray()); });
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject().AddRange(items[0], items[1]); });
        }

        [TestMethod]
        public void AddRangeEnumTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            List<KeyValuePair<string, JsonValue>> items = new List<KeyValuePair<string, JsonValue>>()
            {
                new KeyValuePair<string, JsonValue>(key1, value1),
                new KeyValuePair<string, JsonValue>(key2, value2),
            };

            JsonObject target;
            
            target = new JsonObject();
            target.AddRange(items);
            Assert.AreEqual(2, target.Count);
            ValidateJsonObjectItems(target, key1, value1, key2, value2);

            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { new JsonObject().AddRange(null); });

            items[1] = new KeyValuePair<string, JsonValue>(key2, AnyInstance.DefaultJsonValue);
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { new JsonObject().AddRange(items); });
        }

        [TestMethod]
        public void ClearTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject();
            target.Add(key1, value1);
            target.Clear();
            Assert.AreEqual(0, target.Count);
            Assert.IsFalse(target.ContainsKey(key1));

            target.Add(key2, value2);
            Assert.AreEqual(1, target.Count);
            Assert.IsFalse(target.ContainsKey(key1));
            Assert.IsTrue(target.ContainsKey(key2));
        }

        [TestMethod]
        public void ContainsKeyTest()
        {
            string key1 = AnyInstance.AnyString;
            JsonValue value1 = AnyInstance.AnyJsonValue1;

            JsonObject target = new JsonObject();
            Assert.IsFalse(target.ContainsKey(key1));
            target.Add(key1, value1);
            Assert.IsTrue(target.ContainsKey(key1));
            target.Clear();
            Assert.IsFalse(target.ContainsKey(key1));

            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { new JsonObject().ContainsKey(null); });
        }

        [TestMethod]
        public void CopyToTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            KeyValuePair<string, JsonValue>[] array = new KeyValuePair<string, JsonValue>[target.Count + 1];

            target.CopyTo(array, 1);
            int index1 = key1 == array[1].Key ? 1 : 2;
            int index2 = index1 == 1 ? 2 : 1;

            Assert.AreEqual(key1, array[index1].Key);
            Assert.AreEqual(value1, array[index1].Value);
            Assert.AreEqual(key2, array[index2].Key);
            Assert.AreEqual(value2, array[index2].Value);

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => target.CopyTo(null, 0));
            ExceptionTestHelper.ExpectException<ArgumentOutOfRangeException>(() => target.CopyTo(array, -1));
            ExceptionTestHelper.ExpectException<ArgumentException>(() => target.CopyTo(array, array.Length - target.Count + 1));
        }

        [TestMethod]
        public void CreateFromComplexTypeTest()
        {
            Assert.IsNull(JsonValueExtensions.CreateFrom(null));

            Person anyObject = AnyInstance.AnyPerson;

            JsonObject jv = JsonValueExtensions.CreateFrom(anyObject) as JsonObject;
            Assert.IsNotNull(jv);
            Assert.AreEqual(4, jv.Count);
            foreach (string key in "Name Age Address".Split())
            {
                Assert.IsTrue(jv.ContainsKey(key));
            }

            Assert.AreEqual(AnyInstance.AnyString, (string)jv["Name"]);
            Assert.AreEqual(AnyInstance.AnyInt, (int)jv["Age"]);

            JsonObject nestedObject = jv["Address"] as JsonObject;
            Assert.IsNotNull(nestedObject);
            Assert.AreEqual(3, nestedObject.Count);
            foreach (string key in "Street City State".Split())
            {
                Assert.IsTrue(nestedObject.ContainsKey(key));
            }

            Assert.AreEqual(Address.AnyStreet, (string)nestedObject["Street"]);
            Assert.AreEqual(Address.AnyCity, (string)nestedObject["City"]);
            Assert.AreEqual(Address.AnyState, (string)nestedObject["State"]);
        }

        [TestMethod]
        public void ReadAsComplexTypeTest()
        {
            JsonObject target = new JsonObject
            {
                { "Name", AnyInstance.AnyString },
                { "Age", AnyInstance.AnyInt },
                { "Address", new JsonObject { { "Street", Address.AnyStreet }, { "City", Address.AnyCity }, { "State", Address.AnyState } } },
            };

            Person person = target.ReadAsType<Person>();
            Assert.AreEqual(AnyInstance.AnyString, person.Name);
            Assert.AreEqual(AnyInstance.AnyInt, person.Age);
            Assert.IsNotNull(person.Address);
            Assert.AreEqual(Address.AnyStreet, person.Address.Street);
            Assert.AreEqual(Address.AnyCity, person.Address.City);
            Assert.AreEqual(Address.AnyState, person.Address.State);
        }

        [TestMethod]
        public void GetEnumeratorTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            IEnumerator<KeyValuePair<string, JsonValue>> enumerator = target.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            bool key1IsFirst = key1 == enumerator.Current.Key;
            if (key1IsFirst)
            {
                Assert.AreEqual(key1, enumerator.Current.Key);
                Assert.AreEqual(value1, enumerator.Current.Value);
            }
            else
            {
                Assert.AreEqual(key2, enumerator.Current.Key);
                Assert.AreEqual(value2, enumerator.Current.Value);
            }

            Assert.IsTrue(enumerator.MoveNext());
            if (key1IsFirst)
            {
                Assert.AreEqual(key2, enumerator.Current.Key);
                Assert.AreEqual(value2, enumerator.Current.Value);
            }
            else
            {
                Assert.AreEqual(key1, enumerator.Current.Key);
                Assert.AreEqual(value1, enumerator.Current.Value);
            }

            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void RemoveTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            Assert.IsTrue(target.ContainsKey(key1));
            Assert.IsTrue(target.ContainsKey(key2));
            Assert.AreEqual(2, target.Count);

            Assert.IsTrue(target.Remove(key2));
            Assert.IsTrue(target.ContainsKey(key1));
            Assert.IsFalse(target.ContainsKey(key2));
            Assert.AreEqual(1, target.Count);

            Assert.IsFalse(target.Remove(key2));
            Assert.IsTrue(target.ContainsKey(key1));
            Assert.IsFalse(target.ContainsKey(key2));
            Assert.AreEqual(1, target.Count);
        }

        [TestMethod]
        public void ToStringTest()
        {
            JsonObject target = new JsonObject();
            JsonValue item1 = AnyInstance.AnyJsonValue1 ?? "not null";
            JsonValue item2 = null;
            JsonValue item3 = AnyInstance.AnyJsonValue2 ?? "not null";
            JsonValue item4 = AnyInstance.AnyJsonValue3 ?? "not null";
            target.Add("item1", item1);
            target.Add("item2", item2);
            target.Add("item3", item3);
            target.Add("", item4);

            string expected = string.Format(CultureInfo.InvariantCulture, "{{\"item1\":{0},\"item2\":null,\"item3\":{1},\"\":{2}}}", item1, item3, item4);
            Assert.AreEqual<string>(expected, target.ToString());

            string json = "{\r\n  \"item1\": \"hello\",\r\n  \"item2\": null,\r\n  \"item3\": [\r\n    1,\r\n    2,\r\n    3\r\n  ],\r\n  \"\": \"notnull\"\r\n}";
            target = JsonValue.Parse(json) as JsonObject;
            json = json.Replace("\r\n", "").Replace(" ", "");
            Assert.AreEqual<string>(json, target.ToString());
        }

        [TestMethod]
        public void ContainsKVPTest()
        {
            JsonObject target = new JsonObject();
            KeyValuePair<string, JsonValue> item = new KeyValuePair<string, JsonValue>(AnyInstance.AnyString, AnyInstance.AnyJsonValue1);
            KeyValuePair<string, JsonValue> item2 = new KeyValuePair<string, JsonValue>(AnyInstance.AnyString2, AnyInstance.AnyJsonValue2);
            target.Add(item);
            Assert.IsTrue(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item));
            Assert.IsFalse(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item2));
        }

        [TestMethod]
        public void RemoveKVPTest()
        {
            JsonObject target = new JsonObject();
            KeyValuePair<string, JsonValue> item1 = new KeyValuePair<string, JsonValue>(AnyInstance.AnyString, AnyInstance.AnyJsonValue1);
            KeyValuePair<string, JsonValue> item2 = new KeyValuePair<string, JsonValue>(AnyInstance.AnyString2, AnyInstance.AnyJsonValue2);
            target.AddRange(item1, item2);

            Assert.AreEqual(2, target.Count);
            Assert.IsTrue(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item1));
            Assert.IsTrue(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item2));

            Assert.IsTrue(((ICollection<KeyValuePair<string, JsonValue>>)target).Remove(item1));
            Assert.AreEqual(1, target.Count);
            Assert.IsFalse(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item1));
            Assert.IsTrue(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item2));

            Assert.IsFalse(((ICollection<KeyValuePair<string, JsonValue>>)target).Remove(item1));
            Assert.AreEqual(1, target.Count);
            Assert.IsFalse(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item1));
            Assert.IsTrue(((ICollection<KeyValuePair<string, JsonValue>>)target).Contains(item2));
        }

        [TestMethod]
        public void GetEnumeratorTest1()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            IEnumerator enumerator = ((IEnumerable)target).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsInstanceOfType(enumerator.Current, typeof(KeyValuePair<string, JsonValue>));
            KeyValuePair<string, JsonValue> current = (KeyValuePair<string, JsonValue>)enumerator.Current;

            bool key1IsFirst = key1 == current.Key;
            if (key1IsFirst)
            {
                Assert.AreEqual(key1, current.Key);
                Assert.AreEqual(value1, current.Value);
            }
            else
            {
                Assert.AreEqual(key2, current.Key);
                Assert.AreEqual(value2, current.Value);
            }

            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsInstanceOfType(enumerator.Current, typeof(KeyValuePair<string, JsonValue>));
            current = (KeyValuePair<string, JsonValue>)enumerator.Current;
            if (key1IsFirst)
            {
                Assert.AreEqual(key2, current.Key);
                Assert.AreEqual(value2, current.Value);
            }
            else
            {
                Assert.AreEqual(key1, current.Key);
                Assert.AreEqual(value1, current.Value);
            }

            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void TryGetValueTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            JsonValue value;
            Assert.IsTrue(target.TryGetValue(key2, out value));
            Assert.AreEqual(value2, value);

            Assert.IsFalse(target.TryGetValue("not a key", out value));
            Assert.IsNull(value);
        }

        [TestMethod]
        public void GetValueOrDefaultTest()
        {
            bool boolValue;
            JsonValue target;
            JsonValue jsonValue;

            Person person = AnyInstance.AnyPerson;
            JsonObject jo = JsonValueExtensions.CreateFrom(person) as JsonObject;

            Assert.AreEqual<int>(person.Age, jo.ValueOrDefault("Age").ReadAs<int>()); // JsonPrimitive
            Assert.AreEqual<string>(person.Address.ToString(), jo.ValueOrDefault("Address").ReadAsType<Address>().ToString()); // JsonObject
            Assert.AreEqual<int>(person.Friends.Count, jo.ValueOrDefault("Friends").Count); // JsonArray

            target = jo.ValueOrDefault("Address").ValueOrDefault("City"); // JsonPrimitive
            Assert.IsNotNull(target);
            Assert.AreEqual<string>(person.Address.City, target.ReadAs<string>());

            target = jo.ValueOrDefault("Address", "City"); // JsonPrimitive
            Assert.IsNotNull(target);
            Assert.AreEqual<string>(person.Address.City, target.ReadAs<string>());

            target = jo.ValueOrDefault("Address").ValueOrDefault("NonExistentProp").ValueOrDefault("NonExistentProp2"); // JsonObject
            Assert.AreEqual(JsonType.Default, target.JsonType);
            Assert.IsNotNull(target);
            Assert.IsFalse(target.TryReadAs<bool>(out boolValue));
            Assert.IsTrue(target.TryReadAs<JsonValue>(out jsonValue));

            target = jo.ValueOrDefault("Address", "NonExistentProp", "NonExistentProp2"); // JsonObject
            Assert.AreEqual(JsonType.Default, target.JsonType);
            Assert.IsNotNull(target);
            Assert.IsFalse(target.TryReadAs<bool>(out boolValue));
            Assert.IsTrue(target.TryReadAs<JsonValue>(out jsonValue));
            Assert.AreSame(target, jsonValue);
        }

        [TestMethod]
        public void CountTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject();
            Assert.AreEqual(0, target.Count);
            target.Add(key1, value1);
            Assert.AreEqual(1, target.Count);
            target.Add(key2, value2);
            Assert.AreEqual(2, target.Count);
            target.Remove(key2);
            Assert.AreEqual(1, target.Count);
        }

        [TestMethod]
        public void ItemTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;
            JsonValue value3 = AnyInstance.AnyJsonValue3;

            JsonObject target;
            
            target = new JsonObject { { key1, value1 }, { key2, value2 } };
            Assert.AreEqual(value1, target[key1]);
            Assert.AreEqual(value2, target[key2]);
            target[key1] = value3;
            Assert.AreEqual(value3, target[key1]);
            Assert.AreEqual(value2, target[key2]);

            ExceptionTestHelper.ExpectException<KeyNotFoundException>(delegate { var o = target["not a key"]; });
            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { var o = target[null]; });
            ExceptionTestHelper.ExpectException<ArgumentNullException>(delegate { target[null] = 123; });
            ExceptionTestHelper.ExpectException<ArgumentException>(delegate { target[key1] = AnyInstance.DefaultJsonValue; });
        }

        [TestMethod]
        public void ChangingEventsTest()
        {
            const string key1 = "first";
            const string key2 = "second";
            const string key3 = "third";
            const string key4 = "fourth";
            const string key5 = "fifth";
            JsonObject jo = new JsonObject
            {
                { key1, AnyInstance.AnyString },
                { key2, AnyInstance.AnyBool },
                { key3, null },
            };

            TestEvents(
                jo,
                obj => obj.Add(key4, 1),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(1, JsonValueChange.Add, key4)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(1, JsonValueChange.Add, key4)),
                });

            TestEvents(
                jo,
                obj => obj[key2] = 2,
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(2, JsonValueChange.Replace, key2)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(AnyInstance.AnyBool, JsonValueChange.Replace, key2)),
                });

            TestEvents(
                jo,
                obj => obj[key5] = 3,
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(3, JsonValueChange.Add, key5)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(3, JsonValueChange.Add, key5)),
                });

            jo.Remove(key4);
            jo.Remove(key5);

            TestEvents(
                jo,
                obj => obj.AddRange(new JsonObject { { key4, AnyInstance.AnyString }, { key5, AnyInstance.AnyDouble } }),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(AnyInstance.AnyString, JsonValueChange.Add, key4)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(AnyInstance.AnyDouble, JsonValueChange.Add, key5)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(AnyInstance.AnyString, JsonValueChange.Add, key4)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(AnyInstance.AnyDouble, JsonValueChange.Add, key5)),
                });

            TestEvents(
                jo,
                obj => obj.Remove(key5),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(AnyInstance.AnyDouble, JsonValueChange.Remove, key5)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(AnyInstance.AnyDouble, JsonValueChange.Remove, key5)),
                });

            TestEvents(
                jo,
                obj => obj.Remove("not there"),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>());

            jo = new JsonObject { { key1, 1 }, { key2, 2 }, { key3, 3 } };

            TestEvents(
                jo,
                obj => obj.Clear(),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(null, JsonValueChange.Clear, null)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(null, JsonValueChange.Clear, null)),
                });

            jo = new JsonObject { { key1, 1 }, { key2, 2 }, { key3, 3 } };
            TestEvents(
                jo,
                obj => ((IDictionary<string, JsonValue>)obj).Remove(new KeyValuePair<string, JsonValue>(key2, jo[key2])),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, jo, new JsonValueChangeEventArgs(2, JsonValueChange.Remove, key2)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, jo, new JsonValueChangeEventArgs(2, JsonValueChange.Remove, key2)),
                });

            TestEvents(
                jo,
                obj => ((IDictionary<string, JsonValue>)obj).Remove(new KeyValuePair<string, JsonValue>("key not in object", jo[key1])),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                });

            TestEvents(
                jo,
                obj => ((IDictionary<string, JsonValue>)obj).Remove(new KeyValuePair<string, JsonValue>(key1, "different object")),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                });

            ExceptionTestHelper.ExpectException<ArgumentNullException>(() => new JsonValueChangeEventArgs(1, JsonValueChange.Add, null));
        }

        [TestMethod]
        public void NestedChangingEventTest()
        {
            const string key1 = "first";

            JsonObject target = new JsonObject { { key1, new JsonArray { 1, 2 } } };
            JsonArray child = target[key1] as JsonArray;
            TestEvents(
                target,
                obj => ((JsonArray)obj[key1]).Add(5),
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>());

            target = new JsonObject();
            child = new JsonArray(1, 2);
            TestEvents(
                target,
                obj =>
                {
                    obj.Add(key1, child);
                    ((JsonArray)obj[key1]).Add(5);
                },
                new List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>>
                {
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(true, target, new JsonValueChangeEventArgs(child, JsonValueChange.Add, key1)),
                    new Tuple<bool, JsonValue, JsonValueChangeEventArgs>(false, target, new JsonValueChangeEventArgs(child, JsonValueChange.Add, key1)),
                });
        }

        [TestMethod]
        public void MultipleListenersTest()
        {
            const string key1 = "first";
            const string key2 = "second";
            const string key3 = "third";

            for (int changingListeners = 0; changingListeners <= 2; changingListeners++)
            {
                for (int changedListeners = 0; changedListeners <= 2; changedListeners++)
                {
                    JsonArrayTest.MultipleListenersTest<JsonObject>(
                        () => new JsonObject { { key1, 1 }, { key2, 2 } },
                        delegate(JsonObject obj)
                        {
                            obj[key2] = "hello";
                            obj.Remove(key1);
                            obj.Add(key3, "world");
                            obj.Clear();
                        },
                        new List<JsonValueChangeEventArgs>
                        {
                            new JsonValueChangeEventArgs("hello", JsonValueChange.Replace, key2),
                            new JsonValueChangeEventArgs(1, JsonValueChange.Remove, key1),
                            new JsonValueChangeEventArgs("world", JsonValueChange.Add, key3),
                            new JsonValueChangeEventArgs(null, JsonValueChange.Clear, null),
                        },
                        new List<JsonValueChangeEventArgs>
                        {
                            new JsonValueChangeEventArgs(2, JsonValueChange.Replace, key2),
                            new JsonValueChangeEventArgs(1, JsonValueChange.Remove, key1),
                            new JsonValueChangeEventArgs("world", JsonValueChange.Add, key3),
                            new JsonValueChangeEventArgs(null, JsonValueChange.Clear, null),
                        },
                        changingListeners,
                        changedListeners);
                }
            }
        }

        [TestMethod]
        public void JsonTypeTest()
        {
            JsonObject target = AnyInstance.AnyJsonObject;
            Assert.AreEqual(JsonType.Object, target.JsonType);
        }

        [TestMethod]
        public void KeysTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            List<string> expected = new List<string> { key1, key2 };
            List<string> actual = new List<string>(target.Keys);

            Assert.AreEqual(expected.Count, actual.Count);

            expected.Sort();
            actual.Sort();
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void ValidationAPITest()
        {
            JsonObject jo = new JsonObject();
            jo.Add("date", new DateTime(2000, 1, 1, 0, 0, 0));
            jo.Add("int", 1);
            jo.Add("double", 1.1);
            jo.Add("string", "12CharString");
            jo.Add("enum", "Number");

            jo.ValidatePresence("date")
              .ValidateEnum("enum", typeof(JsonType))
              .ValidateRange("double", 0.1, 1.2)
              .ValidateRange("int", 0, 2)
              .ValidateRange<DateTime>("date", DateTime.MinValue, DateTime.MaxValue)
              .ValidateRegularExpression("int", "^.+$")
              .ValidateStringLength("string", 15)
              .ValidateStringLength("string", 0, 15)
              .ValidateTypeOf<double>("int")
              .ValidateCustomValidator("string", typeof(MyCustomValidationClass), "IsStringContainsCharSimple")
              .ValidateCustomValidator("string", typeof(MyCustomValidationClass), "IsStringContainsCharComplex");

            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidatePresence("invalidkey"); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateEnum("date", typeof(JsonType)); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateRange("double", 2.2, 3.2); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateRange("int", 2, 3); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateRange<DateTime>("date", DateTime.MaxValue, DateTime.MaxValue); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateRegularExpression("string", "doesnotmatch"); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateStringLength("string", 10); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateStringLength("string", 15, 25); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateTypeOf<double>("date"); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateCustomValidator("enum", typeof(MyCustomValidationClass), "IsStringContainsCharSimple"); });
            ExceptionTestHelper.ExpectException<ValidationException>(delegate { jo.ValidateCustomValidator("enum", typeof(MyCustomValidationClass), "IsStringContainsCharComplex"); });
        }

        [TestMethod]
        public void IsReadOnlyTest()
        {
            JsonObject target = AnyInstance.AnyJsonObject;
            Assert.IsFalse(((ICollection<KeyValuePair<string, JsonValue>>)target).IsReadOnly);
        }

        [TestMethod]
        public void ValuesTest()
        {
            string key1 = AnyInstance.AnyString;
            string key2 = AnyInstance.AnyString2;
            JsonValue value1 = AnyInstance.AnyJsonValue1;
            JsonValue value2 = AnyInstance.AnyJsonValue2;

            JsonObject target = new JsonObject { { key1, value1 }, { key2, value2 } };

            List<JsonValue> values = new List<JsonValue>(target.Values);
            Assert.AreEqual(2, values.Count);
            bool value1IsFirst = value1 == values[0];
            Assert.IsTrue(value1IsFirst || value1 == values[1]);
            Assert.AreEqual(value2, values[value1IsFirst ? 1 : 0]);
        }

        static void ValidateJsonObjectItems(JsonObject jsonObject, params object[] keyValuePairs)
        {
            Dictionary<string, JsonValue> expected = new Dictionary<string, JsonValue>();
            Assert.IsTrue((keyValuePairs.Length % 2) == 0, "Test error");
            for (int i = 0; i < keyValuePairs.Length; i += 2)
            {
                Assert.IsInstanceOfType(keyValuePairs[i], typeof(string), "Test error");
                Assert.IsInstanceOfType(keyValuePairs[i + 1], typeof(JsonValue), "Test error");
                expected.Add((string)keyValuePairs[i], (JsonValue)keyValuePairs[i + 1]);
            }
        }

        static void ValidateJsonObjectItems(JsonObject jsonObject, Dictionary<string, JsonValue> expected)
        {
            Assert.AreEqual(expected.Count, jsonObject.Count);
            foreach (string key in expected.Keys)
            {
                Assert.IsTrue(jsonObject.ContainsKey(key));
                Assert.AreEqual(expected[key], jsonObject[key]);
            }
        }

        static void TestEvents(JsonObject obj, Action<JsonObject> actionToTriggerEvent, List<Tuple<bool, JsonValue, JsonValueChangeEventArgs>> expectedEvents)
        {
            JsonArrayTest.TestEvents<JsonObject>(obj, actionToTriggerEvent, expectedEvents);
        }

        public class MyCustomValidationClass
        {
            public static ValidationResult IsStringContainsCharSimple(JsonValue jv)
            {
                string str = jv.ReadAs<string>();

                if (str.Contains("Char"))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult("String must contain 'Char'");
            }

            public static ValidationResult IsStringContainsCharComplex(JsonValue jv, ValidationContext context)
            {
                JsonValue value = (JsonValue)context.ObjectInstance;

                string strValue;
                if (value[context.MemberName].TryReadAs<string>(out strValue) && strValue.Contains("Char"))
                {
                    return ValidationResult.Success;
                }

                return new ValidationResult("String must contain 'Char'", new List<string> { context.MemberName });
            }
        }
    }
}
