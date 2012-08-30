using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;

namespace SetCloudLogging
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists("Cloud.exe"))
            {
                DisplayLocationError();
            }
            else if (args.Length == 0)
            {
                EnableLogging();
            }
            else if (args.Length != 1)
            {
                DisplayArgumentError();
            }
            else
            {
                string flagArg = args[0];

                int doubleQuoteIdx;
                if ((doubleQuoteIdx = flagArg.IndexOf('\"')) > -1)
                {
                    flagArg = flagArg.Substring(doubleQuoteIdx + 1, flagArg.LastIndexOf('\"') - doubleQuoteIdx - 1);
                }

                if (flagArg.Equals("includeAuthentication", StringComparison.InvariantCultureIgnoreCase))
                {
                    EnableLogging(true);
                }
                else
                {
                    long flagInt;
                    bool flagBool;
                    if (long.TryParse(flagArg, out flagInt))
                    {
                        switch (flagInt)
                        {
                            case 0:
                                DisableLogging();
                                break;
                            case 1:
                                EnableLogging();
                                break;
                            case 2:
                                EnableLogging(true);
                                break;
                            default:
                                DisplayArgumentError();
                                break;
                        }
                    }
                    else if (bool.TryParse(flagArg, out flagBool))
                    {
                        if (flagBool)
                        {
                            EnableLogging();
                        }
                        else
                        {
                            DisableLogging();
                        }
                    }
                }
            }
        }

        private static void DisableLogging()
        {
            Process.Start(new ProcessStartInfo("Cloud.exe", "SetCloudLogging " + 0));

            Console.WriteLine("Logging disabled");
            Exit();
        }

        private static void EnableLogging(bool includeAuthorization = false)
        {
            Process.Start(new ProcessStartInfo("Cloud.exe", "SetCloudLogging " + (includeAuthorization ? "2" : "1")));

            Console.WriteLine("Logging enabled");
            Exit();
        }

        private static void DisplayArgumentError()
        {
            Console.WriteLine("You can only call SetCloudLogging with up to one argument: a \"true\" or \"false\" for how to set the flag");
            Exit();
        }

        private static void DisplayLocationError()
        {
            Console.WriteLine("You must place SetCloudLogging executable within the Cloud directory before running");
            Exit();
        }

        private static void Exit()
        {
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}