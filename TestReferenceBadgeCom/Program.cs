using System;
using System.Collections.Generic;
using System.Text;
using BadgeCOMLib;

namespace TestReferenceBadgeCom
{
    class Program
    {
        static void Main(string[] args)
        {

            PubSubServerClass test = new PubSubServerClass();
            test.Publish(5);
            test.nTestProperty = 10;
            Console.WriteLine("Test property is {0}.", test.nTestProperty);
            
           
        }
    }
}
