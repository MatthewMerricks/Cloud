using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestCatchOutput
{
    class Program
    {
        static void Main(string[] args)
        {
            int finalTest = 0;
            bool foundError = false;

            try
            {
                try
                {
                    MakeError(out foundError);
                }
                finally
                {
                    if (foundError)
                    {
                        finalTest = 1;
                    }
                }
            }
            catch
            {
            }

            Console.WriteLine(finalTest);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static void MakeError(out bool foundError)
        {
            foundError = true;
            throw new Exception();
        }
    }
}
