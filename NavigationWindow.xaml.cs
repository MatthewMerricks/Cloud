using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.IO;
using win_client.SystemTray.TrayIcon;
using win_client.AppDelegate;
using System.Windows.Forms;
using win_client.Model;

namespace win_client
{
    /// <summary>
    /// Interaction logic for NavigationWindow.xaml
    /// </summary>
    public partial class MyNavigationWindow : NavigationWindow, IDisposable
    {
        private TrayIcon m_trayIcon;
        private bool disposed = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public MyNavigationWindow()
        {
            this.NavigationService.Navigated += NavigationService_Navigated;
            this.NavigationService.Navigating += NavigationService_Navigating;

            InitializeComponent();
        }

        /// <summary>
        /// Event handler: Navigated.
        /// </summary>
        public static void NavigationService_Navigated(object sender, NavigationEventArgs e)
        {
            IOnNavigated castContent = e.Content as IOnNavigated;
            if (castContent != null)
            {
                castContent.HandleNavigated(sender, e);
            }
        }

        /// <summary>
        /// Event handler: Navigating.
        /// Ignore F5 refresh.
        /// </summary>
        public static void NavigationService_Navigating(object sender, NavigatingCancelEventArgs e) 
        {
            if (e.NavigationMode == NavigationMode.Refresh)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose() 
        { 
            Dispose(true); 
            GC.SuppressFinalize(this); 
        } 
 
        // Leave out the finalizer altogether if this class doesn't own unmanaged
        // resources itself.
        //~MyNavigationWindow() 
        //{ 
        //    Dispose(false); 
        //} 
 
        // Dispose
        protected virtual void Dispose(bool disposing) 
        { 
            if (!this.disposed) 
            { 
                if (disposing) 
                { 
                    // Free managed resources, if any
                    if (m_trayIcon != null)
                    {
                        m_trayIcon.Dispose();
                        m_trayIcon = null;
                    }
                } 

                // Free native (unmanaged) resources, if any
            } 
            disposed = true; 
        }

        /// <summary>
        /// Wait for the SourceInitialized event to set up the tray icon.
        /// </summary>
        private void Window_SourceInitialized(object sender, EventArgs e)
        {

            // Have to wait for source-initialized event to set up the
            // tray icon, or the windows handle will be null

            // Create the tray-icon manager object, and register for events
            m_trayIcon = new TrayIcon(this);
            m_trayIcon.LeftDoubleClick += new EventHandler(TrayIcon_LeftDoubleClick);

        }

        /// <summary>
        /// Hide the tray icon if the window is visible, and vice versa.
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {

            // If this window is minimized...
            if (WindowState == System.Windows.WindowState.Minimized)
            {

                // Make sure tray icon is visible
                if (!m_trayIcon.IsIconVisible)
                    m_trayIcon.Show(global::win_client.Resources.Resources.SystemTrayIcon, "I'm In The Tray!");

                // No need to put icon in taskbar
                this.ShowInTaskbar = false;

            }
            else
            {

                // Make sure tray icon is hidden
                if (m_trayIcon.IsIconVisible)
                    m_trayIcon.Hide();

                // Need to show icon in taskbar
                this.ShowInTaskbar = true;

            }

        }

        /// <summary>
        /// Event handler: Tray icon left double-click.
        /// </summary>
        void TrayIcon_LeftDoubleClick(object sender, EventArgs e)
        {
            // If window needs opening...
            if (WindowState == System.Windows.WindowState.Minimized)
                WindowState = System.Windows.WindowState.Normal;

        }
    }
}
