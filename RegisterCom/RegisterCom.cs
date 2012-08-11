using System;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Reflection;
using System.IO;
using RegisterCom;

namespace OffLine.Installer
{
    [RunInstaller(true)]
    public partial class RegisterCom : System.Configuration.Install.Installer
    {
        public RegisterCom()
            : base()
        {
            // Attach the 'Committed' event.
            this.Committed += new InstallEventHandler(RegisterCom_Committed);
            // Attach the 'Committing' event.
            this.Committing += new InstallEventHandler(RegisterCom_Committing);

        }
        // Event handler for 'Committing' event.
        private void RegisterCom_Committing(object sender, InstallEventArgs e)
        {
            //Console.WriteLine("");
            //Console.WriteLine("Committing Event occured.");
            //Console.WriteLine("");
        }
        // Event handler for 'Committed' event.
        private void RegisterCom_Committed(object sender, InstallEventArgs e)
        {
            try
            {
                Trace.WriteLine("RegisterCom: Committed event. Entry.");
                Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                Trace.WriteLine("RegisterCom: Committed event. Start Cloud.exe.");
                Process.Start(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\Cloud.exe");
                Trace.WriteLine("RegisterCom: Committed event. After starting Cloud.exe.");
            }
            catch(Exception ex)
            {
                Trace.WriteLine("RegisterCom: Committed event. ERROR.  Exception. Message: {0}.", ex.Message);
            }

            // Exit the application.
            MainProgram.shouldTerminate = true;
        }

        // Override the 'Install' method.
        public override void Install(IDictionary savedState)
        {
            Trace.WriteLine("RegisterCom: Committed event. Install method.  Entry.");
            base.Install(savedState);
        }
        // Override the 'Commit' method.
        public override void Commit(IDictionary savedState)
        {
            Trace.WriteLine("RegisterCom: Committed event. Commit method.  Entry.");
            base.Commit(savedState);
        }
        // Override the 'Rollback' method.
        public override void Rollback(IDictionary savedState)
        {
            Trace.WriteLine("RegisterCom: Committed event. Rollback method.  Entry.");
            base.Rollback(savedState);
        }

    }

}
