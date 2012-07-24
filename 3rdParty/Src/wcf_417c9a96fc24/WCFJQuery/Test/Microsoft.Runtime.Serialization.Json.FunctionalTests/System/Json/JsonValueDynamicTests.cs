namespace System.Json.Test
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.Json;
    using System.Reflection;
    using System.Runtime.Serialization.Json;
    using Microsoft.ServiceModel.Web.Test.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for the dynamic support for <see cref="JsonValue"/>.
    /// </summary>
    [TestClass]
    public class JsonValueDynamicTests
    {
        string teamNameValue = "WCF RIA Base";
        string[] teamMembersValues = { "Carlos", "Chris", "Joe", "Miguel", "Yavor" };

        /// <summary>
        /// Tests for the dynamic getters in <see cref="JsonObject"/> instances.
        /// </summary>
        [TestMethod]
        public void JsonObjectDynamicGetters()
        {
            dynamic team = new JsonObject();
            team["TeamSize"] = this.teamMembersValues.Length;
            team["TeamName"] = this.teamNameValue;
            team["TeamMascots"] = null;
            team["TeamMembers"] = new JsonArray 
            { 
                this.teamMembersValues[0], this.teamMembersValues[1], this.teamMembersValues[2],
                this.teamMembersValues[3], this.teamMembersValues[4] 
            };

            Assert.AreEqual(this.teamMembersValues.Length, (int)team.TeamSize);
            Assert.AreEqual(this.teamNameValue, (string)team.TeamName);
            Assert.IsNotNull(team.TeamMascots);
            Assert.IsTrue(team.TeamMascots is JsonValue); // default

            for (int i = 0; i < this.teamMembersValues.Length; i++)
            {
                Assert.AreEqual(this.teamMembersValues[i], (string)team.TeamMembers[i]);
            }

            for (int i = 0; i < this.teamMembersValues.Length; i++)
            {
                Assert.AreEqual(this.teamMembersValues[i], (string)team.TeamMembers[i]);
            }

            // Negative tests for getters
            JsonValueTests.ExpectException<InvalidCastException>(delegate { int fail = (int)team.NonExistentProp; });
        }

        /// <summary>
        /// Tests for the dynamic setters in <see cref="JsonObject"/> instances.
        /// </summary>
        [TestMethod]
        public void JsonObjectDynamicSetters()
        {
            dynamic team = new JsonObject();
            team.TeamSize = this.teamMembersValues.Length;
            team.TeamName = this.teamNameValue;
            team.TeamMascots = null;
            team.TeamMembers = new JsonArray 
            { 
                this.teamMembersValues[0], this.teamMembersValues[1], this.teamMembersValues[2],
                this.teamMembersValues[3], this.teamMembersValues[4] 
            };

            Assert.AreEqual(this.teamMembersValues.Length, (int)team["TeamSize"]);
            Assert.AreEqual(this.teamNameValue, (string)team["TeamName"]);
            Assert.IsNotNull(team["TeamMascots"]);
            Assert.IsTrue(team["TeamMascots"] is JsonValue);

            for (int i = 0; i < this.teamMembersValues.Length; i++)
            {
                Assert.AreEqual(this.teamMembersValues[i], (string)team["TeamMembers"][i]);
            }

            // Could not come up with negative setter
        }

        /// <summary>
        /// Tests for the dynamic indexers in <see cref="JsonArray"/> instances.
        /// </summary>
        [TestMethod]
        public void JsonArrayDynamicSanity()
        {
            // Sanity test for JsonArray to ensure [] still works even if dynamic
            dynamic people = new JsonArray();
            foreach (string member in this.teamMembersValues)
            {
                people.Add(member);
            }

            Assert.AreEqual(this.teamMembersValues[0], (string)people[0]);
            Assert.AreEqual(this.teamMembersValues[1], (string)people[1]);
            Assert.AreEqual(this.teamMembersValues[2], (string)people[2]);
            Assert.AreEqual(this.teamMembersValues[3], (string)people[3]);
            Assert.AreEqual(this.teamMembersValues[4], (string)people[4]);

            // Note: this test and the above execute the dynamic binder differently.
            for (int i = 0; i < people.Count; i++)
            {
                Assert.AreEqual(this.teamMembersValues[i], (string)people[i]);
            }

            people.Add(this.teamMembersValues.Length);
            people.Add(this.teamNameValue);

            Assert.AreEqual(this.teamMembersValues.Length, (int)people[5]);
            Assert.AreEqual(this.teamNameValue, (string)people[6]);
        }

        /// <summary>
        /// Tests for calling methods in dynamic references to <see cref="JsonValue"/> instances.
        /// </summary>
        [TestMethod]
        public void DynamicMethodCalling()
        {
            JsonObject jo = new JsonObject();
            dynamic dyn = jo;
            dyn.Foo = "bar";
            Assert.AreEqual(1, jo.Count);
            Assert.AreEqual(1, dyn.Count);
            dyn.Remove("Foo");
            Assert.AreEqual(0, jo.Count);
        }

        /// <summary>
        /// Tests for using boolean operators in dynamic references to <see cref="JsonValue"/> instances.
        /// </summary>
        [TestMethod]
        public void DynamicBooleanOperators()
        {
            JsonValue jv;
            dynamic dyn;
            foreach (bool value in new bool[] { true, false })
            {
                jv = value;
                dyn = jv;
                Log.Info("IsTrue, {0}", jv);
                if (dyn)
                {
                    Assert.IsTrue(value, "Boolean evaluation should not enter 'if' clause.");
                }
                else
                {
                    Assert.IsFalse(value, "Boolean evaluation should not enter 'else' clause.");
                }
            }

            foreach (string value in new string[] { "true", "false", "True", "False" })
            {
                bool isTrueValue = value.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                jv = new JsonPrimitive(value);
                dyn = jv;
                Log.Info("IsTrue, {0}", jv);
                if (dyn)
                {
                    Assert.IsTrue(isTrueValue, "Boolean evaluation should not enter 'if' clause.");
                }
                else
                {
                    Assert.IsFalse(isTrueValue, "Boolean evaluation should not enter 'else' clause.");
                }
            }

            foreach (bool first in new bool[] { false, true })
            {
                dynamic dyn1 = new JsonPrimitive(first);
                Log.Info("Negation, {0}", first);
                Assert.AreEqual(!first, !dyn1);
                foreach (bool second in new bool[] { false, true })
                {
                    dynamic dyn2 = new JsonPrimitive(second);
                    Log.Info("Boolean AND, {0} && {1}", first, second);
                    Assert.AreEqual(first && second, (bool)(dyn1 && dyn2), string.Format("Boolean AND: {0} && {1}", first, second));
                    Log.Info("Boolean OR, {0} && {1}", first, second);
                    Assert.AreEqual(first || second, (bool)(dyn1 || dyn2), string.Format("Boolean OR: {0} && {1}", first, second));
                }
            }

            Log.Info("Invalid boolean operator usage");
            dynamic boolDyn = new JsonPrimitive(true);
            dynamic intDyn = new JsonPrimitive(1);
            dynamic strDyn = new JsonPrimitive("hello");

            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", !intDyn); });

            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", !strDyn); });
            JsonValueTests.ExpectException<InvalidCastException>(() => { Log.Info("{0}", intDyn && intDyn); });
            JsonValueTests.ExpectException<InvalidCastException>(() => { Log.Info("{0}", intDyn || true); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", boolDyn && 1); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", boolDyn && intDyn); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", boolDyn && "hello"); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", boolDyn && strDyn); });
            JsonValueTests.ExpectException<FormatException>(() => { Log.Info("{0}", strDyn && boolDyn); });
            JsonValueTests.ExpectException<FormatException>(() => { Log.Info("{0}", strDyn || true); });

            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", !intDyn.NotHere); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", !intDyn.NotHere && true); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info("{0}", !intDyn.NotHere || false); });
        }

        /// <summary>
        /// Tests for using relational operators in dynamic references to <see cref="JsonValue"/> instances.
        /// </summary>
        [TestMethod]
        public void DynamicRelationalOperators()
        {
            JsonValue jv = new JsonObject { { "one", 1 }, { "one_point_two", 1.2 }, { "decimal_one_point_one", 1.1m }, { "trueValue", true }, { "str", "hello" } };
            dynamic dyn = jv;
            JsonValue defaultJsonValue = jv.ValueOrDefault(-1);

            Log.Info("Equality");
            Assert.IsTrue(dyn.one == 1);
            Assert.IsTrue(dyn.one_point_two == 1.2);
            Assert.IsFalse(dyn.one == 1.2);
            Assert.IsFalse(dyn.one_point_two == 1);
            Assert.IsFalse(dyn.one == 2);
            Assert.IsFalse(dyn.one_point_two == 1.3);
            Assert.IsTrue(dyn.one == 1m);
            Assert.IsFalse(dyn.one == 2m);
            Assert.IsTrue(dyn.decimal_one_point_one == 1.1m);

            Assert.IsTrue(dyn.NotHere == null);
            Assert.IsTrue(dyn.NotHere == dyn.NotHere);
            Assert.IsTrue(dyn.NotHere == defaultJsonValue);
            // DISABLED, 197375, Assert.IsFalse(dyn.NotHere == 1);
            Assert.IsFalse(dyn.NotHere == jv);

            Log.Info("Inequality");
            Assert.IsFalse(dyn.one != 1);
            Assert.IsFalse(dyn.one_point_two != 1.2);
            Assert.IsTrue(dyn.one != 1.2);
            Assert.IsTrue(dyn.one_point_two != 1);
            Assert.IsTrue(dyn.one != 2);
            Assert.IsTrue(dyn.one_point_two != 1.3);
            Assert.IsFalse(dyn.one != 1m);
            Assert.IsTrue(dyn.one != 2m);

            Assert.IsFalse(dyn.NotHere != null);
            Assert.IsFalse(dyn.NotHere != dyn.NotHere);
            Assert.IsFalse(dyn.NotHere != defaultJsonValue);
            // DISABLED, 197375, Assert.IsTrue(dyn.NotHere != 1);
            Assert.IsTrue(dyn.NotHere != jv);

            Log.Info("Less than");
            Assert.IsTrue(dyn.one < 2);
            Assert.IsFalse(dyn.one < 1);
            Assert.IsFalse(dyn.one < 0);
            Assert.IsTrue(dyn.one_point_two < 1.3);
            Assert.IsFalse(dyn.one_point_two < 1.2);
            Assert.IsFalse(dyn.one_point_two < 1.1);

            Assert.IsTrue(dyn.one < 1.1);
            Assert.AreEqual(1 < 1.0, dyn.one < 1.0);
            Assert.IsFalse(dyn.one < 0.9);
            Assert.IsTrue(dyn.one_point_two < 2);
            Assert.IsFalse(dyn.one_point_two < 1);
            Assert.AreEqual(1.2 < 1.2f, dyn.one_point_two < 1.2f);

            Log.Info("Greater than");
            Assert.IsFalse(dyn.one > 2);
            Assert.IsFalse(dyn.one > 1);
            Assert.IsTrue(dyn.one > 0);
            Assert.IsFalse(dyn.one_point_two > 1.3);
            Assert.IsFalse(dyn.one_point_two > 1.2);
            Assert.IsTrue(dyn.one_point_two > 1.1);

            Assert.IsFalse(dyn.one > 1.1);
            Assert.AreEqual(1 > 1.0, dyn.one > 1.0);
            Assert.IsTrue(dyn.one > 0.9);
            Assert.IsFalse(dyn.one_point_two > 2);
            Assert.IsTrue(dyn.one_point_two > 1);
            Assert.AreEqual(1.2 > 1.2f, dyn.one_point_two > 1.2f);

            Log.Info("Less than or equals");
            Assert.IsTrue(dyn.one <= 2);
            Assert.IsTrue(dyn.one <= 1);
            Assert.IsFalse(dyn.one <= 0);
            Assert.IsTrue(dyn.one_point_two <= 1.3);
            Assert.IsTrue(dyn.one_point_two <= 1.2);
            Assert.IsFalse(dyn.one_point_two <= 1.1);

            Assert.IsTrue(dyn.one <= 1.1);
            Assert.AreEqual(1 <= 1.0, dyn.one <= 1.0);
            Assert.IsFalse(dyn.one <= 0.9);
            Assert.IsTrue(dyn.one_point_two <= 2);
            Assert.IsFalse(dyn.one_point_two <= 1);
            Assert.AreEqual(1.2 <= 1.2f, dyn.one_point_two <= 1.2f);

            Log.Info("Greater than or equals");
            Assert.IsFalse(dyn.one >= 2);
            Assert.IsTrue(dyn.one >= 1);
            Assert.IsTrue(dyn.one >= 0);
            Assert.IsFalse(dyn.one_point_two >= 1.3);
            Assert.IsTrue(dyn.one_point_two >= 1.2);
            Assert.IsTrue(dyn.one_point_two >= 1.1);

            Assert.IsFalse(dyn.one >= 1.1);
            Assert.AreEqual(1 >= 1.0, dyn.one >= 1.0);
            Assert.IsTrue(dyn.one >= 0.9);
            Assert.IsFalse(dyn.one_point_two >= 2);
            Assert.IsTrue(dyn.one_point_two >= 1);
            Assert.AreEqual(1.2 >= 1.2f, dyn.one_point_two >= 1.2f);

            Log.Info("Invalid number conversions");
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info(dyn.decimal_one_point_one == 1.1); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info(dyn.one != (uint)2); });

            Log.Info("Invalid data types for relational operators");
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info(dyn.trueValue >= dyn.trueValue); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info(dyn.NotHere < dyn.NotHere); });
            JsonValueTests.ExpectException<InvalidOperationException>(() => { Log.Info(dyn.str < "Jello"); });

            // DISABLED, 197315
            Log.Info("Conversions from string");
            jv = new JsonObject { { "one", "1" }, { "twelve_point_two", "1.22e1" } };
            dyn = jv;
            Assert.IsTrue(dyn.one == 1);
            Assert.IsTrue(dyn.twelve_point_two == 1.22e1);
            Assert.IsTrue(dyn.one >= 0.5f);
            Assert.IsTrue(dyn.twelve_point_two <= 13);
            Assert.IsTrue(dyn.one < 2);
            Assert.AreEqual(dyn.twelve_point_two.ReadAs<int>() > 12, dyn.twelve_point_two > 12);
        }

        /// <summary>
        /// Tests for using arithmetic operators in dynamic references to <see cref="JsonValue"/> instances.
        /// </summary>
        [TestMethod]
        public void ArithmeticOperators()
        {
            int seed = MethodBase.GetCurrentMethod().Name.GetHashCode();
            Log.Info("Seed: {0}", seed);
            Random rndGen = new Random(seed);
            int i1 = rndGen.Next(-10000, 10000);
            int i2 = rndGen.Next(-10000, 10000);
            JsonValue jv1 = i1;
            JsonValue jv2 = i2;
            Log.Info("jv1 = {0}, jv2 = {1}", jv1, jv2);
            dynamic dyn1 = jv1;
            dynamic dyn2 = jv2;

            string str1 = i1.ToString(CultureInfo.InvariantCulture);
            string str2 = i2.ToString(CultureInfo.InvariantCulture);
            JsonValue jvstr1 = str1;
            JsonValue jvstr2 = str2;

            Log.Info("Unary +");
            Assert.AreEqual<int>(+i1, +dyn1);
            Assert.AreEqual<int>(+i2, +dyn2);

            Log.Info("Unary -");
            Assert.AreEqual<int>(-i1, -dyn1);
            Assert.AreEqual<int>(-i2, -dyn2);

            Log.Info("Unary ~ (bitwise NOT)");
            Assert.AreEqual<int>(~i1, ~dyn1);
            Assert.AreEqual<int>(~i2, ~dyn2);

            Log.Info("Binary +: {0}", i1 + i2);
            Assert.AreEqual<int>(i1 + i2, dyn1 + dyn2);
            Assert.AreEqual<int>(i1 + i2, dyn2 + dyn1);
            Assert.AreEqual<int>(i1 + i2, dyn1 + i2);
            Assert.AreEqual<int>(i1 + i2, dyn2 + i1);

            // DISABLED, 197394
            // Assert.AreEqual<int>(i1 + i2, dyn1 + str2);
            // Assert.AreEqual<int>(i1 + i2, dyn1 + jvstr2);

            Log.Info("Binary -: {0}, {1}", i1 - i2, i2 - i1);
            Assert.AreEqual<int>(i1 - i2, dyn1 - dyn2);
            Assert.AreEqual<int>(i2 - i1, dyn2 - dyn1);
            Assert.AreEqual<int>(i1 - i2, dyn1 - i2);
            Assert.AreEqual<int>(i2 - i1, dyn2 - i1);

            Log.Info("Binary *: {0}", i1 * i2);
            Assert.AreEqual<int>(i1 * i2, dyn1 * dyn2);
            Assert.AreEqual<int>(i1 * i2, dyn2 * dyn1);
            Assert.AreEqual<int>(i1 * i2, dyn1 * i2);
            Assert.AreEqual<int>(i1 * i2, dyn2 * i1);

            while (i1 == 0)
            {
                i1 = rndGen.Next(-10000, 10000);
                jv1 = i1;
                dyn1 = jv1;
                Log.Info("Using new (non-zero) i1 value: {0}", i1);
            }

            while (i2 == 0)
            {
                i2 = rndGen.Next(-10000, 10000);
                jv2 = i2;
                dyn2 = jv2;
                Log.Info("Using new (non-zero) i2 value: {0}", i2);
            }

            Log.Info("Binary / (integer division): {0}, {1}", i1 / i2, i2 / i1);
            Assert.AreEqual<int>(i1 / i2, dyn1 / dyn2);
            Assert.AreEqual<int>(i2 / i1, dyn2 / dyn1);
            Assert.AreEqual<int>(i1 / i2, dyn1 / i2);
            Assert.AreEqual<int>(i2 / i1, dyn2 / i1);

            Log.Info("Binary % (modulo): {0}, {1}", i1 % i2, i2 % i1);
            Assert.AreEqual<int>(i1 % i2, dyn1 % dyn2);
            Assert.AreEqual<int>(i2 % i1, dyn2 % dyn1);
            Assert.AreEqual<int>(i1 % i2, dyn1 % i2);
            Assert.AreEqual<int>(i2 % i1, dyn2 % i1);

            Log.Info("Binary & (bitwise AND): {0}", i1 & i2);
            Assert.AreEqual<int>(i1 & i2, dyn1 & dyn2);
            Assert.AreEqual<int>(i1 & i2, dyn2 & dyn1);
            Assert.AreEqual<int>(i1 & i2, dyn1 & i2);
            Assert.AreEqual<int>(i1 & i2, dyn2 & i1);

            Log.Info("Binary | (bitwise OR): {0}", i1 | i2);
            Assert.AreEqual<int>(i1 | i2, dyn1 | dyn2);
            Assert.AreEqual<int>(i1 | i2, dyn2 | dyn1);
            Assert.AreEqual<int>(i1 | i2, dyn1 | i2);
            Assert.AreEqual<int>(i1 | i2, dyn2 | i1);

            Log.Info("Binary ^ (bitwise XOR): {0}", i1 ^ i2);
            Assert.AreEqual<int>(i1 ^ i2, dyn1 ^ dyn2);
            Assert.AreEqual<int>(i1 ^ i2, dyn2 ^ dyn1);
            Assert.AreEqual<int>(i1 ^ i2, dyn1 ^ i2);
            Assert.AreEqual<int>(i1 ^ i2, dyn2 ^ i1);

            i1 = rndGen.Next(1, 10);
            i2 = rndGen.Next(1, 10);
            jv1 = i1;
            jv2 = i2;
            dyn1 = jv1;
            dyn2 = jv2;
            Log.Info("New i1, i2: {0}, {1}", i1, i2);

            Log.Info("Left shift: {0}", i1 << i2);
            Assert.AreEqual<int>(i1 << i2, dyn1 << dyn2);
            Assert.AreEqual<int>(i1 << i2, dyn1 << i2);

            i1 = i1 << i2;
            jv1 = i1;
            dyn1 = jv1;
            Log.Info("New i1: {0}", i1);
            Log.Info("Right shift: {0}", i1 >> i2);
            Assert.AreEqual<int>(i1 >> i2, dyn1 >> dyn2);
            Assert.AreEqual<int>(i1 >> i2, dyn1 >> i2);

            i2 += 4;
            jv2 = i2;
            dyn2 = jv2;
            Log.Info("New i2: {0}", i2);
            Log.Info("Right shift: {0}", i1 >> i2);
            Assert.AreEqual<int>(i1 >> i2, dyn1 >> dyn2);
            Assert.AreEqual<int>(i1 >> i2, dyn1 >> i2);
        }

        /// <summary>
        /// Tests for conversions between data types in arithmetic operations.
        /// </summary>
        [TestMethod]
        public void ArithmeticConversion()
        {
            JsonObject jo = new JsonObject
            {
                { "byteVal", (byte)10 },
                { "sbyteVal", (sbyte)10 },
                { "shortVal", (short)10 },
                { "ushortVal", (ushort)10 },
                { "intVal", 10 },
                { "uintVal", (uint)10 },
                { "longVal", 10L },
                { "ulongVal", (ulong)10 },
                { "charVal", (char)10 },
                { "decimalVal", 10m },
                { "doubleVal", 10.0 },
                { "floatVal", 10f },
            };
            dynamic dyn = jo;

            Log.Info("Conversion from byte");
            // DISABLED, 197387, ValidateResult<int>(dyn.byteVal + (byte)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.byteVal + (sbyte)10));
            ValidateResult<short>(dyn.byteVal + (short)10, 20);
            ValidateResult<ushort>(dyn.byteVal + (ushort)10, 20);
            ValidateResult<int>(dyn.byteVal + (int)10, 20);
            ValidateResult<uint>(dyn.byteVal + (uint)10, 20);
            ValidateResult<long>(dyn.byteVal + 10L, 20);
            ValidateResult<ulong>(dyn.byteVal + (ulong)10, 20);
            ValidateResult<decimal>(dyn.byteVal + 10m, 20);
            ValidateResult<float>(dyn.byteVal + 10f, 20);
            ValidateResult<double>(dyn.byteVal + 10.0, 20);

            Log.Info("Conversion from sbyte");
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.sbyteVal + (byte)10));
            // DISABLED, 197387, ValidateResult<int>(dyn.sbyteVal + (sbyte)10, 20);
            ValidateResult<short>(dyn.sbyteVal + (short)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.sbyteVal + (ushort)10));
            ValidateResult<int>(dyn.sbyteVal + (int)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.sbyteVal + (uint)10));
            ValidateResult<long>(dyn.sbyteVal + 10L, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.sbyteVal + (ulong)10));
            ValidateResult<decimal>(dyn.sbyteVal + 10m, 20);
            ValidateResult<float>(dyn.sbyteVal + 10f, 20);
            ValidateResult<double>(dyn.sbyteVal + 10.0, 20);

            Log.Info("Conversion from short");
            ValidateResult<short>(dyn.shortVal + (byte)10, 20);
            ValidateResult<short>(dyn.shortVal + (sbyte)10, 20);
            ValidateResult<short>(dyn.shortVal + (short)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.shortVal + (ushort)10));
            ValidateResult<int>(dyn.shortVal + (int)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.shortVal + (uint)10));
            ValidateResult<long>(dyn.shortVal + 10L, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.shortVal + (ulong)10));
            ValidateResult<decimal>(dyn.shortVal + 10m, 20);
            ValidateResult<float>(dyn.shortVal + 10f, 20);
            ValidateResult<double>(dyn.shortVal + 10.0, 20);

            Log.Info("Conversion from ushort");
            ValidateResult<ushort>(dyn.ushortVal + (byte)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.ushortVal + (sbyte)10));
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.ushortVal + (short)10));
            ValidateResult<ushort>(dyn.ushortVal + (ushort)10, 20);
            ValidateResult<int>(dyn.ushortVal + (int)10, 20);
            ValidateResult<uint>(dyn.ushortVal + (uint)10, 20);
            ValidateResult<long>(dyn.ushortVal + 10L, 20);
            ValidateResult<ulong>(dyn.ushortVal + (ulong)10, 20);
            ValidateResult<decimal>(dyn.ushortVal + 10m, 20);
            ValidateResult<float>(dyn.ushortVal + 10f, 20);
            ValidateResult<double>(dyn.ushortVal + 10.0, 20);

            Log.Info("Conversion from int");
            ValidateResult<int>(dyn.intVal + (byte)10, 20);
            ValidateResult<int>(dyn.intVal + (sbyte)10, 20);
            ValidateResult<int>(dyn.intVal + (short)10, 20);
            ValidateResult<int>(dyn.intVal + (ushort)10, 20);
            ValidateResult<int>(dyn.intVal + (int)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.intVal + (uint)10));
            ValidateResult<long>(dyn.intVal + 10L, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.intVal + (ulong)10));
            ValidateResult<decimal>(dyn.intVal + 10m, 20);
            ValidateResult<float>(dyn.intVal + 10f, 20);
            ValidateResult<double>(dyn.intVal + 10.0, 20);

            Log.Info("Conversion from uint");
            ValidateResult<uint>(dyn.uintVal + (byte)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.uintVal + (sbyte)10));
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.uintVal + (short)10));
            ValidateResult<uint>(dyn.uintVal + (ushort)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.uintVal + (int)10));
            ValidateResult<uint>(dyn.uintVal + (uint)10, 20);
            ValidateResult<long>(dyn.uintVal + 10L, 20);
            ValidateResult<ulong>(dyn.uintVal + (ulong)10, 20);
            ValidateResult<decimal>(dyn.uintVal + 10m, 20);
            ValidateResult<float>(dyn.uintVal + 10f, 20);
            ValidateResult<double>(dyn.uintVal + 10.0, 20);

            Log.Info("Conversion from long");
            ValidateResult<long>(dyn.longVal + (byte)10, 20);
            ValidateResult<long>(dyn.longVal + (sbyte)10, 20);
            ValidateResult<long>(dyn.longVal + (short)10, 20);
            ValidateResult<long>(dyn.longVal + (ushort)10, 20);
            ValidateResult<long>(dyn.longVal + (int)10, 20);
            ValidateResult<long>(dyn.longVal + (uint)10, 20);
            ValidateResult<long>(dyn.longVal + 10L, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.longVal + (ulong)10));
            ValidateResult<decimal>(dyn.longVal + 10m, 20);
            ValidateResult<float>(dyn.longVal + 10f, 20);
            ValidateResult<double>(dyn.longVal + 10.0, 20);

            Log.Info("Conversion from ulong");
            ValidateResult<ulong>(dyn.ulongVal + (byte)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.ulongVal + (sbyte)10));
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.ulongVal + (short)10));
            ValidateResult<ulong>(dyn.ulongVal + (ushort)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.ulongVal + (int)10));
            ValidateResult<ulong>(dyn.ulongVal + (uint)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.ulongVal + (long)10));
            ValidateResult<ulong>(dyn.ulongVal + (ulong)10, 20);
            ValidateResult<decimal>(dyn.ulongVal + 10m, 20);
            ValidateResult<float>(dyn.ulongVal + 10f, 20);
            ValidateResult<double>(dyn.ulongVal + 10.0, 20);

            Log.Info("Conversion from float");
            ValidateResult<float>(dyn.floatVal + (byte)10, 20);
            ValidateResult<float>(dyn.floatVal + (sbyte)10, 20);
            ValidateResult<float>(dyn.floatVal + (short)10, 20);
            ValidateResult<float>(dyn.floatVal + (ushort)10, 20);
            ValidateResult<float>(dyn.floatVal + (int)10, 20);
            ValidateResult<float>(dyn.floatVal + (uint)10, 20);
            ValidateResult<float>(dyn.floatVal + 10L, 20);
            ValidateResult<float>(dyn.floatVal + (ulong)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.floatVal + 10m));
            ValidateResult<float>(dyn.floatVal + 10f, 20);
            ValidateResult<double>(dyn.floatVal + 10.0, 20);

            Log.Info("Conversion from double");
            ValidateResult<double>(dyn.doubleVal + (byte)10, 20);
            ValidateResult<double>(dyn.doubleVal + (sbyte)10, 20);
            ValidateResult<double>(dyn.doubleVal + (short)10, 20);
            ValidateResult<double>(dyn.doubleVal + (ushort)10, 20);
            ValidateResult<double>(dyn.doubleVal + (int)10, 20);
            ValidateResult<double>(dyn.doubleVal + (uint)10, 20);
            ValidateResult<double>(dyn.doubleVal + 10L, 20);
            ValidateResult<double>(dyn.doubleVal + (ulong)10, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.doubleVal + 10m));
            ValidateResult<double>(dyn.doubleVal + 10f, 20);
            ValidateResult<double>(dyn.doubleVal + 10.0, 20);

            Log.Info("Conversion from decimal");
            ValidateResult<decimal>(dyn.decimalVal + (byte)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + (sbyte)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + (short)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + (ushort)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + (int)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + (uint)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + 10L, 20);
            ValidateResult<decimal>(dyn.decimalVal + (ulong)10, 20);
            ValidateResult<decimal>(dyn.decimalVal + 10m, 20);
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.decimalVal + 10f));
            JsonValueTests.ExpectException<InvalidOperationException>(() => Log.Info("{0}", dyn.decimalVal + 10.0));
        }

        /// <summary>
        /// Tests for implicit casts between dynamic references to <see cref="JsonPrimitive"/> instances
        /// and the supported CLR types.
        /// </summary>
        [TestMethod]
        public void ImplicitPrimitiveCastTests()
        {
            DateTime now = DateTime.Now;
            int seed = now.Year * 10000 + now.Month * 100 + now.Day;
            Log.Info("Seed: {0}", seed);
            Random rndGen = new Random(seed);
            int intValue = rndGen.Next(1, 127);
            Log.Info("Value: {0}", intValue);

            uint uintValue = (uint)intValue;
            short shortValue = (short)intValue;
            ushort ushortValue = (ushort)intValue;
            long longValue = (long)intValue;
            ulong ulongValue = (ulong)intValue;
            byte byteValue = (byte)intValue;
            sbyte sbyteValue = (sbyte)intValue;
            float floatValue = (float)intValue;
            double doubleValue = (double)intValue;
            decimal decimalValue = (decimal)intValue;
            string stringValue = intValue.ToString(CultureInfo.InvariantCulture);

            dynamic dyn = new JsonObject
            {
                { "Byte", byteValue },
                { "SByte", sbyteValue },
                { "Int16", shortValue },
                { "UInt16", ushortValue },
                { "Int32", intValue },
                { "UInt32", uintValue },
                { "Int64", longValue },
                { "UInt64", ulongValue },
                { "Double", doubleValue },
                { "Single", floatValue },
                { "Decimal", decimalValue },
                { "String", stringValue },
                { "True", "true" },
                { "False", "false" },
            };

            Log.Info("dyn: {0}", dyn);

            Log.Info("Casts to Byte");

            byte byteFromByte = dyn.Byte;
            byte byteFromSByte = dyn.SByte;
            byte byteFromShort = dyn.Int16;
            byte byteFromUShort = dyn.UInt16;
            byte byteFromInt = dyn.Int32;
            byte byteFromUInt = dyn.UInt32;
            byte byteFromLong = dyn.Int64;
            byte byteFromULong = dyn.UInt64;
            byte byteFromDouble = dyn.Double;
            byte byteFromFloat = dyn.Single;
            byte byteFromDecimal = dyn.Decimal;
            byte byteFromString = dyn.String;

            Assert.AreEqual<byte>(byteValue, byteFromByte);
            Assert.AreEqual<byte>(byteValue, byteFromSByte);
            Assert.AreEqual<byte>(byteValue, byteFromShort);
            Assert.AreEqual<byte>(byteValue, byteFromUShort);
            Assert.AreEqual<byte>(byteValue, byteFromInt);
            Assert.AreEqual<byte>(byteValue, byteFromUInt);
            Assert.AreEqual<byte>(byteValue, byteFromLong);
            Assert.AreEqual<byte>(byteValue, byteFromULong);
            Assert.AreEqual<byte>(byteValue, byteFromDouble);
            Assert.AreEqual<byte>(byteValue, byteFromFloat);
            Assert.AreEqual<byte>(byteValue, byteFromDecimal);
            Assert.AreEqual<byte>(byteValue, byteFromString);

            Log.Info("Casts to SByte");

            sbyte sbyteFromByte = dyn.Byte;
            sbyte sbyteFromSByte = dyn.SByte;
            sbyte sbyteFromShort = dyn.Int16;
            sbyte sbyteFromUShort = dyn.UInt16;
            sbyte sbyteFromInt = dyn.Int32;
            sbyte sbyteFromUInt = dyn.UInt32;
            sbyte sbyteFromLong = dyn.Int64;
            sbyte sbyteFromULong = dyn.UInt64;
            sbyte sbyteFromDouble = dyn.Double;
            sbyte sbyteFromFloat = dyn.Single;
            sbyte sbyteFromDecimal = dyn.Decimal;
            sbyte sbyteFromString = dyn.String;

            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromByte);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromSByte);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromShort);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromUShort);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromInt);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromUInt);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromLong);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromULong);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromDouble);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromFloat);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromDecimal);
            Assert.AreEqual<sbyte>(sbyteValue, sbyteFromString);

            Log.Info("Casts to Short");

            short shortFromByte = dyn.Byte;
            short shortFromSByte = dyn.SByte;
            short shortFromShort = dyn.Int16;
            short shortFromUShort = dyn.UInt16;
            short shortFromInt = dyn.Int32;
            short shortFromUInt = dyn.UInt32;
            short shortFromLong = dyn.Int64;
            short shortFromULong = dyn.UInt64;
            short shortFromDouble = dyn.Double;
            short shortFromFloat = dyn.Single;
            short shortFromDecimal = dyn.Decimal;
            short shortFromString = dyn.String;

            Assert.AreEqual<short>(shortValue, shortFromByte);
            Assert.AreEqual<short>(shortValue, shortFromSByte);
            Assert.AreEqual<short>(shortValue, shortFromShort);
            Assert.AreEqual<short>(shortValue, shortFromUShort);
            Assert.AreEqual<short>(shortValue, shortFromInt);
            Assert.AreEqual<short>(shortValue, shortFromUInt);
            Assert.AreEqual<short>(shortValue, shortFromLong);
            Assert.AreEqual<short>(shortValue, shortFromULong);
            Assert.AreEqual<short>(shortValue, shortFromDouble);
            Assert.AreEqual<short>(shortValue, shortFromFloat);
            Assert.AreEqual<short>(shortValue, shortFromDecimal);
            Assert.AreEqual<short>(shortValue, shortFromString);

            Log.Info("Casts to UShort");

            ushort ushortFromByte = dyn.Byte;
            ushort ushortFromSByte = dyn.SByte;
            ushort ushortFromShort = dyn.Int16;
            ushort ushortFromUShort = dyn.UInt16;
            ushort ushortFromInt = dyn.Int32;
            ushort ushortFromUInt = dyn.UInt32;
            ushort ushortFromLong = dyn.Int64;
            ushort ushortFromULong = dyn.UInt64;
            ushort ushortFromDouble = dyn.Double;
            ushort ushortFromFloat = dyn.Single;
            ushort ushortFromDecimal = dyn.Decimal;
            ushort ushortFromString = dyn.String;

            Assert.AreEqual<ushort>(ushortValue, ushortFromByte);
            Assert.AreEqual<ushort>(ushortValue, ushortFromSByte);
            Assert.AreEqual<ushort>(ushortValue, ushortFromShort);
            Assert.AreEqual<ushort>(ushortValue, ushortFromUShort);
            Assert.AreEqual<ushort>(ushortValue, ushortFromInt);
            Assert.AreEqual<ushort>(ushortValue, ushortFromUInt);
            Assert.AreEqual<ushort>(ushortValue, ushortFromLong);
            Assert.AreEqual<ushort>(ushortValue, ushortFromULong);
            Assert.AreEqual<ushort>(ushortValue, ushortFromDouble);
            Assert.AreEqual<ushort>(ushortValue, ushortFromFloat);
            Assert.AreEqual<ushort>(ushortValue, ushortFromDecimal);
            Assert.AreEqual<ushort>(ushortValue, ushortFromString);

            Log.Info("Casts to Int");

            int intFromByte = dyn.Byte;
            int intFromSByte = dyn.SByte;
            int intFromShort = dyn.Int16;
            int intFromUShort = dyn.UInt16;
            int intFromInt = dyn.Int32;
            int intFromUInt = dyn.UInt32;
            int intFromLong = dyn.Int64;
            int intFromULong = dyn.UInt64;
            int intFromDouble = dyn.Double;
            int intFromFloat = dyn.Single;
            int intFromDecimal = dyn.Decimal;
            int intFromString = dyn.String;

            Assert.AreEqual<int>(intValue, intFromByte);
            Assert.AreEqual<int>(intValue, intFromSByte);
            Assert.AreEqual<int>(intValue, intFromShort);
            Assert.AreEqual<int>(intValue, intFromUShort);
            Assert.AreEqual<int>(intValue, intFromInt);
            Assert.AreEqual<int>(intValue, intFromUInt);
            Assert.AreEqual<int>(intValue, intFromLong);
            Assert.AreEqual<int>(intValue, intFromULong);
            Assert.AreEqual<int>(intValue, intFromDouble);
            Assert.AreEqual<int>(intValue, intFromFloat);
            Assert.AreEqual<int>(intValue, intFromDecimal);
            Assert.AreEqual<int>(intValue, intFromString);

            Log.Info("Casts to UInt");

            uint uintFromByte = dyn.Byte;
            uint uintFromSByte = dyn.SByte;
            uint uintFromShort = dyn.Int16;
            uint uintFromUShort = dyn.UInt16;
            uint uintFromInt = dyn.Int32;
            uint uintFromUInt = dyn.UInt32;
            uint uintFromLong = dyn.Int64;
            uint uintFromULong = dyn.UInt64;
            uint uintFromDouble = dyn.Double;
            uint uintFromFloat = dyn.Single;
            uint uintFromDecimal = dyn.Decimal;
            uint uintFromString = dyn.String;

            Assert.AreEqual<uint>(uintValue, uintFromByte);
            Assert.AreEqual<uint>(uintValue, uintFromSByte);
            Assert.AreEqual<uint>(uintValue, uintFromShort);
            Assert.AreEqual<uint>(uintValue, uintFromUShort);
            Assert.AreEqual<uint>(uintValue, uintFromInt);
            Assert.AreEqual<uint>(uintValue, uintFromUInt);
            Assert.AreEqual<uint>(uintValue, uintFromLong);
            Assert.AreEqual<uint>(uintValue, uintFromULong);
            Assert.AreEqual<uint>(uintValue, uintFromDouble);
            Assert.AreEqual<uint>(uintValue, uintFromFloat);
            Assert.AreEqual<uint>(uintValue, uintFromDecimal);
            Assert.AreEqual<uint>(uintValue, uintFromString);

            Log.Info("Casts to Long");

            long longFromByte = dyn.Byte;
            long longFromSByte = dyn.SByte;
            long longFromShort = dyn.Int16;
            long longFromUShort = dyn.UInt16;
            long longFromInt = dyn.Int32;
            long longFromUInt = dyn.UInt32;
            long longFromLong = dyn.Int64;
            long longFromULong = dyn.UInt64;
            long longFromDouble = dyn.Double;
            long longFromFloat = dyn.Single;
            long longFromDecimal = dyn.Decimal;
            long longFromString = dyn.String;

            Assert.AreEqual<long>(longValue, longFromByte);
            Assert.AreEqual<long>(longValue, longFromSByte);
            Assert.AreEqual<long>(longValue, longFromShort);
            Assert.AreEqual<long>(longValue, longFromUShort);
            Assert.AreEqual<long>(longValue, longFromInt);
            Assert.AreEqual<long>(longValue, longFromUInt);
            Assert.AreEqual<long>(longValue, longFromLong);
            Assert.AreEqual<long>(longValue, longFromULong);
            Assert.AreEqual<long>(longValue, longFromDouble);
            Assert.AreEqual<long>(longValue, longFromFloat);
            Assert.AreEqual<long>(longValue, longFromDecimal);
            Assert.AreEqual<long>(longValue, longFromString);

            Log.Info("Casts to ULong");

            ulong ulongFromByte = dyn.Byte;
            ulong ulongFromSByte = dyn.SByte;
            ulong ulongFromShort = dyn.Int16;
            ulong ulongFromUShort = dyn.UInt16;
            ulong ulongFromInt = dyn.Int32;
            ulong ulongFromUInt = dyn.UInt32;
            ulong ulongFromLong = dyn.Int64;
            ulong ulongFromULong = dyn.UInt64;
            ulong ulongFromDouble = dyn.Double;
            ulong ulongFromFloat = dyn.Single;
            ulong ulongFromDecimal = dyn.Decimal;
            ulong ulongFromString = dyn.String;

            Assert.AreEqual<ulong>(ulongValue, ulongFromByte);
            Assert.AreEqual<ulong>(ulongValue, ulongFromSByte);
            Assert.AreEqual<ulong>(ulongValue, ulongFromShort);
            Assert.AreEqual<ulong>(ulongValue, ulongFromUShort);
            Assert.AreEqual<ulong>(ulongValue, ulongFromInt);
            Assert.AreEqual<ulong>(ulongValue, ulongFromUInt);
            Assert.AreEqual<ulong>(ulongValue, ulongFromLong);
            Assert.AreEqual<ulong>(ulongValue, ulongFromULong);
            Assert.AreEqual<ulong>(ulongValue, ulongFromDouble);
            Assert.AreEqual<ulong>(ulongValue, ulongFromFloat);
            Assert.AreEqual<ulong>(ulongValue, ulongFromDecimal);
            Assert.AreEqual<ulong>(ulongValue, ulongFromString);

            Log.Info("Casts to Float");

            float floatFromByte = dyn.Byte;
            float floatFromSByte = dyn.SByte;
            float floatFromShort = dyn.Int16;
            float floatFromUShort = dyn.UInt16;
            float floatFromInt = dyn.Int32;
            float floatFromUInt = dyn.UInt32;
            float floatFromLong = dyn.Int64;
            float floatFromULong = dyn.UInt64;
            float floatFromDouble = dyn.Double;
            float floatFromFloat = dyn.Single;
            float floatFromDecimal = dyn.Decimal;
            float floatFromString = dyn.String;

            Assert.AreEqual<float>(floatValue, floatFromByte);
            Assert.AreEqual<float>(floatValue, floatFromSByte);
            Assert.AreEqual<float>(floatValue, floatFromShort);
            Assert.AreEqual<float>(floatValue, floatFromUShort);
            Assert.AreEqual<float>(floatValue, floatFromInt);
            Assert.AreEqual<float>(floatValue, floatFromUInt);
            Assert.AreEqual<float>(floatValue, floatFromLong);
            Assert.AreEqual<float>(floatValue, floatFromULong);
            Assert.AreEqual<float>(floatValue, floatFromDouble);
            Assert.AreEqual<float>(floatValue, floatFromFloat);
            Assert.AreEqual<float>(floatValue, floatFromDecimal);
            Assert.AreEqual<float>(floatValue, floatFromString);

            Log.Info("Casts to Double");

            double doubleFromByte = dyn.Byte;
            double doubleFromSByte = dyn.SByte;
            double doubleFromShort = dyn.Int16;
            double doubleFromUShort = dyn.UInt16;
            double doubleFromInt = dyn.Int32;
            double doubleFromUInt = dyn.UInt32;
            double doubleFromLong = dyn.Int64;
            double doubleFromULong = dyn.UInt64;
            double doubleFromDouble = dyn.Double;
            double doubleFromFloat = dyn.Single;
            double doubleFromDecimal = dyn.Decimal;
            double doubleFromString = dyn.String;

            Assert.AreEqual<double>(doubleValue, doubleFromByte);
            Assert.AreEqual<double>(doubleValue, doubleFromSByte);
            Assert.AreEqual<double>(doubleValue, doubleFromShort);
            Assert.AreEqual<double>(doubleValue, doubleFromUShort);
            Assert.AreEqual<double>(doubleValue, doubleFromInt);
            Assert.AreEqual<double>(doubleValue, doubleFromUInt);
            Assert.AreEqual<double>(doubleValue, doubleFromLong);
            Assert.AreEqual<double>(doubleValue, doubleFromULong);
            Assert.AreEqual<double>(doubleValue, doubleFromDouble);
            Assert.AreEqual<double>(doubleValue, doubleFromFloat);
            Assert.AreEqual<double>(doubleValue, doubleFromDecimal);
            Assert.AreEqual<double>(doubleValue, doubleFromString);

            Log.Info("Casts to Decimal");

            decimal decimalFromByte = dyn.Byte;
            decimal decimalFromSByte = dyn.SByte;
            decimal decimalFromShort = dyn.Int16;
            decimal decimalFromUShort = dyn.UInt16;
            decimal decimalFromInt = dyn.Int32;
            decimal decimalFromUInt = dyn.UInt32;
            decimal decimalFromLong = dyn.Int64;
            decimal decimalFromULong = dyn.UInt64;
            decimal decimalFromDouble = dyn.Double;
            decimal decimalFromFloat = dyn.Single;
            decimal decimalFromDecimal = dyn.Decimal;
            decimal decimalFromString = dyn.String;

            Assert.AreEqual<decimal>(decimalValue, decimalFromByte);
            Assert.AreEqual<decimal>(decimalValue, decimalFromSByte);
            Assert.AreEqual<decimal>(decimalValue, decimalFromShort);
            Assert.AreEqual<decimal>(decimalValue, decimalFromUShort);
            Assert.AreEqual<decimal>(decimalValue, decimalFromInt);
            Assert.AreEqual<decimal>(decimalValue, decimalFromUInt);
            Assert.AreEqual<decimal>(decimalValue, decimalFromLong);
            Assert.AreEqual<decimal>(decimalValue, decimalFromULong);
            Assert.AreEqual<decimal>(decimalValue, decimalFromDouble);
            Assert.AreEqual<decimal>(decimalValue, decimalFromFloat);
            Assert.AreEqual<decimal>(decimalValue, decimalFromDecimal);
            Assert.AreEqual<decimal>(decimalValue, decimalFromString);

            Log.Info("Casts to String");

            string stringFromByte = dyn.Byte;
            string stringFromSByte = dyn.SByte;
            string stringFromShort = dyn.Int16;
            string stringFromUShort = dyn.UInt16;
            string stringFromInt = dyn.Int32;
            string stringFromUInt = dyn.UInt32;
            string stringFromLong = dyn.Int64;
            string stringFromULong = dyn.UInt64;
            string stringFromDouble = dyn.Double;
            string stringFromFloat = dyn.Single;
            string stringFromDecimal = dyn.Decimal;
            string stringFromString = dyn.String;

            Assert.AreEqual<string>(stringValue, stringFromByte);
            Assert.AreEqual<string>(stringValue, stringFromSByte);
            Assert.AreEqual<string>(stringValue, stringFromShort);
            Assert.AreEqual<string>(stringValue, stringFromUShort);
            Assert.AreEqual<string>(stringValue, stringFromInt);
            Assert.AreEqual<string>(stringValue, stringFromUInt);
            Assert.AreEqual<string>(stringValue, stringFromLong);
            Assert.AreEqual<string>(stringValue, stringFromULong);
            Assert.AreEqual<string>(stringValue, stringFromDouble);
            Assert.AreEqual<string>(stringValue, stringFromFloat);
            Assert.AreEqual<string>(stringValue, stringFromDecimal);
            Assert.AreEqual<string>(stringValue, stringFromString);

            Log.Info("Casts to Boolean");

            bool bTrue = dyn.True;
            bool bFalse = dyn.False;
            Assert.IsTrue(bTrue);
            Assert.IsFalse(bFalse);
        }

        /// <summary>
        /// Test for creating a JsonValue from a deep-nested dynamic object.
        /// </summary>
        [TestMethod]
        public void CreateFromDeepNestedDynamic()
        {
            int count = 10000;
            string expected = "";

            dynamic dyn = new TestDynamicObject();
            dynamic cur = dyn;

            for (int i = 0; i < count; i++)
            {
                expected += "{\"" + i + "\":";
                cur[i.ToString()] = new TestDynamicObject();
                cur = cur[i.ToString()];
            }

            expected += "{}";

            for (int i = 0; i < count; i++)
            {
                expected += "}";
            }

            JsonValue jv = JsonValueExtensions.CreateFrom(dyn);
            Assert.AreEqual<string>(expected, jv.ToString());
        }

        private void ValidateResult<ResultType>(dynamic value, ResultType expectedResult)
        {
            Assert.IsInstanceOfType(value, typeof(ResultType));
            Assert.AreEqual<ResultType>(expectedResult, (ResultType)value);
        }

        /// <summary>
        /// Concrete DynamicObject class for testing purposes.
        /// </summary>
        internal class TestDynamicObject : DynamicObject
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

            public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
            {
                string key = indexes[0].ToString();

                if (_values.ContainsKey(key))
                {
                    _values[key] = value;
                }
                else
                {
                    _values.Add(key, value);
                }
                return true;
            }

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                string key = indexes[0].ToString();

                if (_values.ContainsKey(key))
                {
                    result = _values[key];
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }
    }
}
