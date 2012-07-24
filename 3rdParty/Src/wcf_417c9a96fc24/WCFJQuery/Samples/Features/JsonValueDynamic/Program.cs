// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonValueDynamic
{
    using System;
    using System.Json;

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
                    ""IsMarried"" : false,
                    ""OrderHistory"" : null,
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

            dynamic ja = JsonValue.Parse(customers);
            Console.WriteLine(ja.ToString() + Environment.NewLine);

            Console.WriteLine("Construct a JsonArray using code");

            ja = new JsonArray 
            {
                new JsonObject 
                {
                    { "ID", new Guid("538a868a-c575-4fc9-9a3e-e1e1e68c70c5") },
                    { "Name", "Yavor" },
                    { "DOB", new DateTime(1984, 01, 17) },
                    { "OrderAmount", 10000 },
                    { "IsMarried", false },
                    { "OrderHistory", null },
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

            Console.WriteLine(ja.ToString() + Environment.NewLine);

            Console.WriteLine("Get a value");

            string name = ja[0].Name;
            float orderAmount = ja[0].OrderAmount;
            Console.WriteLine("{0}, {1}" + Environment.NewLine, name, orderAmount);

            /// Values are cast to the appropriate type implicitly, so
            /// there is no need to cast as (T) or do ReadAs<T>()

            Console.WriteLine("Set a value");

            ja[0].Name = "Yavor Georgiev";
            ja[0].ID = new Guid();
            ja[0].OrderAmount = 30000;

            // Add a new value
            ja[0].Phone = 4251234567;
            Console.WriteLine(ja[0].ToString() + Environment.NewLine);

            Console.WriteLine("Delete a value");

            // Unfortunately dynamics don't have a removal syntax we have to 
            // drop down to the Remove method, which is not discoverable because
            // dynamic types don't generate IntelliSense. For an example of using 
            // this method, check the JsonValueBasic sample
            ja[0].Remove("Phone");
            Console.WriteLine(ja[0].ToString() + Environment.NewLine);

            Console.WriteLine("Working with values");

            bool isTrue = ja[0].OrderHistory == null;
            Console.WriteLine("Null comparison {0}", isTrue);

            // TODO Currently we have a bug where if you add a null to a type
            // it will cause ToObjectArray (used later) to throw an exception
            ja[0].Remove("OrderHistory");

            isTrue = ja[0].OrderAmount > 20000;
            Console.WriteLine("Relational operators {0}", isTrue);

            isTrue = ja[0].OrderAmount + 10000 == 40000;
            Console.WriteLine("Arithmetic operators {0}", isTrue);

            isTrue = !ja[0].IsMarried;
            Console.WriteLine("Logical operators {0}" + Environment.NewLine, isTrue);

            Console.WriteLine("Get a value parsed from inside a JSON string");

            Guid id = ja[0].ID;
            Console.WriteLine(id + Environment.NewLine);

            Console.WriteLine("Get DateTime parsed from inside a JSON string");

            // Relative date format - relative to this machine time
            DateTime dob1 = ja[0].DOB;

            // Absolute date format - fixed to reference UTC
            DateTime dob2 = ja[1].DOB;
            DateTime dob3 = ja[2].DOB;
            Console.WriteLine("{0}, {1}, {2}" + Environment.NewLine, dob1, dob2, dob3);

            Console.WriteLine("Get DateTimeOffset parsed from inside a JSON string");

            // Relative date format - relative to this machine time
            DateTimeOffset dobOffset1 = ja[0].DOB;

            // Absolute date format - fixed to reference UTC
            DateTimeOffset dobOffset2 = ja[1].DOB;
            DateTimeOffset dobOffset3 = ja[2].DOB;
            Console.WriteLine("{0}, {1}, {2}" + Environment.NewLine, dobOffset1, dobOffset2, dobOffset3);

            Console.WriteLine("Try get a value parsed from inside a JSON string " +
                "but encounter wrong format in the string");

            try
            {
                int wrongType = ja[0].ID;
                Console.WriteLine(wrongType);
            }
            catch (InvalidCastException)
            {
                Console.WriteLine("Could not parse the required format and threw an exception");
            }

            Console.WriteLine();

            /// If you prefer to not use an exception-based model to handle invalid
            /// cast/parse operations, TryReadAs<T>(out T valueOfT) and ReadAs<T>(T fallback)
            /// are still available to you, however they are not discoverable because
            /// dynamic types don't generate IntelliSense. For an example of using 
            /// these methods, check the JsonValueBasic sample.

            Console.WriteLine("Index into the type");

            Guid firstFriend = ja[0].Friends[0];
            Console.WriteLine(firstFriend + Environment.NewLine);

            try
            {
                Guid wrongIndex = ja[0].Friends[10];
                Console.WriteLine(wrongIndex);
            }
            catch (InvalidCastException)
            {
                Console.WriteLine("Tried to index into type but specified invalid index " +
                    "and only got an exception when trying to access the value itself.");
            }
            
            /// Indexing into a type will never yield an exception along the lines of
            /// KeyNotFoundException or ArgumentOutOfRangeException, because under the
            /// covers this uses the ValueOrDefault method. To learn more about that method 
            /// see the JsonValueBasic sample. If the user indexes into an invalid element,
            /// they will only get an exception when they try to access the value itself
            /// This is nice since you don't have to worry about catching exceptions due to 
            /// invalid indices - just catch the final exception when the value is accessed.

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
