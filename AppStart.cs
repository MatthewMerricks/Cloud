using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace win_client
{
    public class AppStart : System.Windows.Application
    {
        /// <summary>
        /// Application Entry Point.
        /// </summary>
        [System.MTAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public static void Main()
        {
            win_client.App app = new win_client.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
