namespace Microsoft.ServiceModel.Web.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Json;
    using System.Reflection;

    public static class AnyInstance
    {
        public const bool AnyBool = true;
        public const string AnyString = "hello";
        public const string AnyString2 = "world";
        public const char AnyChar = 'c';
        public const int AnyInt = 123456789;
        public const uint AnyUInt = 3123456789;
        public const long AnyLong = 123456789012345L;
        public const ulong AnyULong = ulong.MaxValue;
        public const short AnyShort = -12345;
        public const ushort AnyUShort = 40000;
        public const byte AnyByte = 0xDC;
        public const sbyte AnySByte = -34;
        public const double AnyDouble = 123.45;
        public const float AnyFloat = 23.4f;
        public const decimal AnyDecimal = 1234.5678m;
        public static readonly Guid AnyGuid = new Guid(0x11223344, 0x5566, 0x7788, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x00);
        public static readonly DateTime AnyDateTime = new DateTime(2010, 02, 15, 22, 45, 20, DateTimeKind.Utc);
        public static readonly DateTimeOffset AnyDateTimeOffset = new DateTimeOffset(2010, 2, 5, 15, 45, 20, TimeSpan.FromHours(-3));
        public static readonly Uri AnyUri = new Uri("http://tempuri.org/");

        public static readonly JsonArray AnyJsonArray = new JsonArray { 1, 2, 3 };
        public static readonly JsonObject AnyJsonObject = new JsonObject { { "one", 1 }, { "two", 2 } };
        public static readonly JsonPrimitive AnyJsonPrimitive = new JsonPrimitive("hello");

        public static readonly JsonValue AnyJsonValue1 = AnyJsonPrimitive;
        public static readonly JsonValue AnyJsonValue2 = AnyJsonArray;
        public static readonly JsonValue AnyJsonValue3 = null;

        public static readonly JsonValue DefaultJsonValue = GetDefaultJsonValue();

        public static readonly Person AnyPerson = Person.CreateSample();
        public static readonly Address AnyAddress = Address.CreateSample();

        public static JsonValue GetDefaultJsonValue()
        {
            PropertyInfo propInfo = typeof(JsonValue).GetProperty("DefaultInstance", BindingFlags.Static | BindingFlags.NonPublic);
            return propInfo.GetValue(null, null) as JsonValue;
        }
    }

    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public Address Address { get; set; }

        public List<Person> Friends { get; set; }

        public static Person CreateSample()
        {
            Person anyObject = new Person
            {
                Name = AnyInstance.AnyString,
                Age = AnyInstance.AnyInt,
                Address = Address.CreateSample(),
                Friends = new List<Person> { new Person { Name = "Bill Gates", Age = 23, Address = Address.CreateSample() }, new Person { Name = "Steve Ballmer", Age = 19, Address = Address.CreateSample() } }
            };

            return anyObject;
        }

        public string FriendsToString()
        {
            string s = "";

            foreach (Person p in this.Friends)
            {
                s += p + ",";
            }

            return s;
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, [{2}], Friends=[{3}]", this.Name, this.Age, this.Address, this.FriendsToString());
        }
    }

    public class Address
    {
        public const string AnyStreet = "123 1st Ave";

        public const string AnyCity = "Springfield";

        public const string AnyState = "ZZ";

        public string Street { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public static Address CreateSample()
        {
            Address address = new Address
            {
                Street = AnyStreet,
                City = AnyCity,
                State = AnyState,
            };

            return address;
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", this.Street, this.City, this.State);
        }
    }
}
