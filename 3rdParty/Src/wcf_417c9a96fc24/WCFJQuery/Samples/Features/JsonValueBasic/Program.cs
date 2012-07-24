// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonValueBasic
{
    using System;
    using System.Json;
    using System.Runtime.Serialization.Json;

    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("Construct a JsonArray by parsing a JSON string");

            string customers = @"
            [
                {   ""ID"" : ""538a868a-c575-4fc9-9a3e-e1e1e68c70c5"",
                    ""Name"" : ""Yavor"",
                    ""DOB"" : ""1984-01-17"",
                    ""OrderAmount"" : 1e+4,
                    ""Friends"" :   [
                                        ""007cf155-7fb4-4070-9d78-ade638df44c7"",
                                        ""91c50a40-7ade-4c37-a88f-3b7e066644dc""
                                    ]
                },
                {   ""ID"" : ""007cf155-7fb4-4070-9d78-ade638df44c7"",
                    ""Name"" : ""Joe"",
                    ""DOB"" : ""1983-02-18T11:22:33Z"",
                    ""OrderAmount"" : 50000,
                    ""Friends"" :   [
                                        ""91c50a40-7ade-4c37-a88f-3b7e066644dc""
                                    ]
                },
                {   ""ID"" : ""91c50a40-7ade-4c37-a88f-3b7e066644dc"",
                    ""Name"" : ""Miguel"",
                    ""DOB"" : ""Mon, 20 Nov 1995 19:12:08 -0500"",
                    ""OrderAmount"" : 25.3e3,
                    ""Friends"" :   [
                                        ""007cf155-7fb4-4070-9d78-ade638df44c7""
                                    ]
                }
            ]";

            JsonArray ja = JsonValue.Parse(customers) as JsonArray;
            Console.WriteLine(ja + Environment.NewLine);

            Console.WriteLine("Construct a JsonArray using code");

            ja = new JsonArray 
            {
                new JsonObject 
                {
                    { "ID", new Guid("538a868a-c575-4fc9-9a3e-e1e1e68c70c5") },
                    { "Name", "Yavor" },
                    { "DOB", new DateTime(1984, 01, 17) },
                    { "OrderAmount", 10000 },
                    { "Friends", new JsonArray
                        {
                            new Guid("007cf155-7fb4-4070-9d78-ade638df44c7"),
                            new Guid("91c50a40-7ade-4c37-a88f-3b7e066644dc")
                        }
                    }
                },
                new JsonObject 
                {
                    { "ID", new Guid("007cf155-7fb4-4070-9d78-ade638df44c7") },
                    { "Name", "Joe" },
                    { "DOB", new DateTimeOffset(1983, 02, 18, 11, 22, 33, TimeSpan.FromHours(0)) },
                    { "OrderAmount", 50000 },
                    { "Friends", new JsonArray
                        {
                            new Guid("91c50a40-7ade-4c37-a88f-3b7e066644dc")
                        }
                    }
                },
                new JsonObject 
                {
                    { "ID", new Guid("91c50a40-7ade-4c37-a88f-3b7e066644dc") },
                    { "Name", "Miguel" },
                    { "DOB", new DateTimeOffset(1995, 11, 20, 19, 12, 08, TimeSpan.FromHours(-5)) },
                    { "OrderAmount", 25300 },
                    { "Friends", new JsonArray
                        {
                            new Guid("007cf155-7fb4-4070-9d78-ade638df44c7")
                        }
                    }
                }
            };

            Console.WriteLine(ja + Environment.NewLine);

            Console.WriteLine("Get a value in two equivalent ways");

            string name = (string)ja[0]["Name"];
            float orderAmount = (float)ja[0]["OrderAmount"];
            Console.WriteLine("{0}, {1}", name, orderAmount);
            
            // ReadAs<T> is equivalent to an explicit (T) cast, but yields 
            // more fluent code so we will use it for the rest of this example
            name = ja[0]["Name"].ReadAs<string>();
            orderAmount = ja[0]["OrderAmount"].ReadAs<float>();
            Console.WriteLine("{0}, {1}" + Environment.NewLine, name, orderAmount);

            Console.WriteLine("Get the raw value");
            
            // The explicit (T) cast and ReadAs<T> let you get the value as any 
            // type you want. If you just want the value without any casting
            // (maybe if you are trying to clone the JsonObject) you can use 
            // this approach
            object orderAmountObject = (ja[0]["OrderAmount"] as JsonPrimitive).Value;
            Console.WriteLine(orderAmountObject + Environment.NewLine);

            Console.WriteLine("Set a value");

            ja[0]["Name"] = "Yavor Georgiev";
            ja[0]["ID"] = new Guid();
            ja[0]["OrderAmount"] = 30000;

            // Add a new value
            ja[0]["Phone"] = 4251234567; 
            Console.WriteLine(ja[0] + Environment.NewLine);

            Console.WriteLine("Delete a value");

            (ja[0] as JsonObject).Remove("Phone");
            Console.WriteLine(ja[0] + Environment.NewLine);

            Console.WriteLine("Get a value parsed from inside a JSON string");
            
            Guid id = ja[0]["ID"].ReadAs<Guid>();
            Console.WriteLine(id + Environment.NewLine);

            Console.WriteLine("Get DateTime parsed from inside a JSON string");
            
            // Relative date format - relative to this machine time
            DateTime dob1 = ja[0]["DOB"].ReadAs<DateTime>();

            // Absolute date format - fixed to reference UTC
            DateTime dob2 = ja[1]["DOB"].ReadAs<DateTime>();
            DateTime dob3 = ja[2]["DOB"].ReadAs<DateTime>();
            Console.WriteLine("{0}, {1}, {2}" + Environment.NewLine, dob1, dob2, dob3);

            Console.WriteLine("Get DateTimeOffset parsed from inside a JSON string");
            
            // Relative date format - relative to this machine time
            DateTimeOffset dobOffset1 = ja[0]["DOB"].ReadAs<DateTimeOffset>();

            // Absolute date format - fixed to reference UTC
            DateTimeOffset dobOffset2 = ja[1]["DOB"].ReadAs<DateTimeOffset>();
            DateTimeOffset dobOffset3 = ja[2]["DOB"].ReadAs<DateTimeOffset>();
            Console.WriteLine("{0}, {1}, {2}" + Environment.NewLine, dobOffset1, dobOffset2, dobOffset3);

            Console.WriteLine("Try get a value parsed from inside a JSON string " +
                "but encounter wrong format in the string");
            
            int wrongType;

            // Throwing exceptions is bad form
            try
            {
                wrongType = ja[0]["ID"].ReadAs<int>();
            }
            catch (FormatException)
            {
                Console.WriteLine("ReadAs could not parse the required format and threw an exception");
            }

            if (ja[0]["ID"].TryReadAs<Guid>(out id))
            {
                Console.WriteLine(id);
            }
            
            if (!ja[0]["ID"].TryReadAs<int>(out wrongType))
            {
                Console.WriteLine("TryReadAs could not parse the required format and returned false");
            }

            Console.WriteLine();

            Console.WriteLine("Get a value from a string, providing a fallback value");
            
            id = ja[0]["ID"].ReadAs<Guid>(new Guid());
            wrongType = ja[0]["ID"].ReadAs<int>(0);
            Console.WriteLine("{0} {1}" + Environment.NewLine, id, wrongType);

            Console.WriteLine("Index into the type");

            Guid firstFriend = ja[0]["Friends"][0].ReadAs<Guid>(new Guid());
            Console.WriteLine(firstFriend);

            Guid wrongIndex;
            try
            {
                wrongIndex = ja[0]["Friends"][10].ReadAs<Guid>(new Guid());
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine("Tried to index into type but specified invalid index and got an exception");
            }

            Console.WriteLine();

            Console.WriteLine("Index into the type while avoiding throwing exceptions");

            firstFriend = ja.ValueOrDefault(0, "Friends", 0).ReadAs<Guid>(new Guid());
            Console.WriteLine(firstFriend);
             
            if (!ja.ValueOrDefault(0, "Friends", 10).TryReadAs<Guid>(out wrongIndex))
            {
                Console.WriteLine("Successfully indexed using safe indexer, but got false for the value");
            }

            wrongIndex = ja.ValueOrDefault(0, "Friends", 10).ReadAs<Guid>(new Guid());
            Console.WriteLine(wrongIndex + Environment.NewLine);

            Console.WriteLine("Strip out JsonValue hierarchy to get regular CLR types");

            object[] unwrapped = ja.ToObjectArray();
            Console.WriteLine(
                "Type of parent {0} and type of children {1}" + Environment.NewLine,
                unwrapped.GetType(), 
                unwrapped[0].GetType());

            /// Similarly you can use ToDictionary() if the class is a JsonObject

            Console.ReadLine();
        }
    }
}
