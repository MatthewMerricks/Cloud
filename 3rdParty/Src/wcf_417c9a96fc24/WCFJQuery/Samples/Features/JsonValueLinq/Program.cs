// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace JsonValueLinq
{
    using System;
    using System.Collections.Generic;
    using System.Json;
    using System.Linq;

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

            Console.WriteLine("Element operators - first");

            var first = ja.FirstOrDefault<JsonValue>();
            Console.WriteLine(first + Environment.NewLine);

            Console.WriteLine("Restriction operators - where");

            var bigOrders = from JsonValue value in ja
                        where value["OrderAmount"].ReadAs<int>(0) > 30000
                        select value;
            Console.WriteLine(bigOrders.Count());
            
            bigOrders = ja.Where<JsonValue>(x => 
                x["OrderAmount"].ReadAs<int>(0) > 30000);
            Console.WriteLine(bigOrders.Count());

            // In some cases you are writing a LINQ query over a piece 
            // of JSON that came from a third party and you may not be
            // guaranteed the shape of it. You can iterate over any 
            // type in the JsonValue family using the KeyValuePair
            // iterator, and you're guaranteed that it is supported and that
            // it will be empty if you don't have a JsonObject or JsonArray
            bigOrders = from KeyValuePair<string, JsonValue> value in ja
                        where value.Value.ValueOrDefault("OrderAmount").ReadAs<int>(0) > 30000
                        select value.Value;
            Console.WriteLine(bigOrders.Count() + Environment.NewLine);

            Console.WriteLine("Partition operators - skip/take");

            var second = ja.Skip<JsonValue>(1).Take<JsonValue>(1);
            Console.WriteLine(second.Count() + Environment.NewLine);

            Console.WriteLine("Conversion operators");

            var old = from JsonValue value in ja
                      where value["DOB"].ReadAs<DateTime>(DateTime.Now) < new DateTime(1990, 1, 1)
                      select value;

            Console.WriteLine(old.ToArray<JsonValue>().Count());
            Console.WriteLine(old.ToJsonArray().Count + Environment.NewLine);

            Console.ReadLine();
        }
    }
}
