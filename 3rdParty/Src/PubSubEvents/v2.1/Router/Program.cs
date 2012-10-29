using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Diagnostics;
using System.Text;

namespace Microsoft.WebSolutionsPlatform.Event
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
        static void Main(string[] args)
		{
            if (args.Length > 0)
            {
                Router eventsvc = new Router();

                eventsvc.Start();

                Console.WriteLine(" --- Press Enter to Quit ---");

                Console.ReadLine();

                eventsvc.Stop();

                return;
            }

			ServiceBase[] ServicesToRun;

			// More than one user Service may run within the same process. To add
			// another service to this process, change the following line to
			// create a second service object. For example,
			//
			//   ServicesToRun = new ServiceBase[] {new Service1(), new MySecondUserService()};
			//
			ServicesToRun = new ServiceBase[] { new Router() };

			ServiceBase.Run(ServicesToRun);
		}
	}
}