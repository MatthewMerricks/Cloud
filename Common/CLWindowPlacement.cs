//
//  CLWindowPlacement.cs
//  Cloud Windows
//  From: http://blogs.msdn.com/b/davidrickard/archive/2010/03/09/saving-window-size-and-location-in-wpf-and-winforms.aspx
//
//  Created by BobS.
//  Changes Copyright (c) Cloud.com. All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using CloudApiPublic.Model;
using CloudApiPublic.Support;
using win_client.Static;
using System.Windows.Interop;

namespace win_client.Common
{
    public static class WindowPlacement
    {
        private static Encoding encoding = new UTF8Encoding();
        private static XmlSerializer serializer = new XmlSerializer(typeof(NativeMethods.WINDOWPLACEMENT));
        private static CLTrace _trace = CLTrace.Instance;

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;

        /// <summary>
        /// Deserialize the window placement information from an XML string.
        /// </summary>
        /// <param name="placementXml">The XML string.</param>
        /// <param name="windowPlacement">The output WINDOWPLACEMENT structure.</param>
        /// <returns>bool: true: Success.</returns>
        internal static bool ExtractWindowPlacementInfo(string placementXml, ref NativeMethods.WINDOWPLACEMENT placement)
        {
            if (string.IsNullOrEmpty(placementXml))
            {
                _trace.writeToLog(1, "CLWindowPlacement: ExtractWindowPlacementInfo: ERROR: placementXML is null.");
                return false;
            }

            byte[] xmlBytes = encoding.GetBytes(placementXml);

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(xmlBytes))
                {
                    placement = (NativeMethods.WINDOWPLACEMENT)serializer.Deserialize(memoryStream);
                }

                placement.length = Marshal.SizeOf(typeof(NativeMethods.WINDOWPLACEMENT));
                placement.flags = 0;
                placement.showCmd = (placement.showCmd == SW_SHOWMINIMIZED ? SW_SHOWNORMAL : placement.showCmd);
            }
            catch (InvalidOperationException)
            {
                // Parsing placement XML failed. Fail silently.
                CLError error = null;
                _trace.writeToLog(1, "CLWindowPlacement: ExtractWindowPlacementInfo: ERROR: Exception.  Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extract placement information from an XML string and use it to set the placement information to a window.
        /// </summary>
        /// <param name="windowHandle">The window handle.</param>
        /// <param name="placementXml">The XML string.</param>
        public static void SetPlacement(IntPtr windowHandle, string placementXml)
        {
            NativeMethods.WINDOWPLACEMENT placement = new NativeMethods.WINDOWPLACEMENT();
            bool rc = ExtractWindowPlacementInfo(placementXml, ref placement);
            if (!rc)
            {
                _trace.writeToLog(1, "CLWindowPlacement: SetPlacement: ERROR: from ExtractWindowPlacementInfo.");
                return;
            }

            try
            {
                NativeMethods.SetWindowPlacement(windowHandle, ref placement);
            }
            catch (InvalidOperationException)
            {
                // Parsing placement XML failed. Fail silently.
                CLError error = null;
                _trace.writeToLog(1, "CLWindowPlacement: SetPlacement: ERROR: Exception.  Msg: {0}, Code: {1}.", error.errorDescription, error.errorCode);
            }
        }

        public static string GetPlacement(IntPtr windowHandle)
        {
            NativeMethods.WINDOWPLACEMENT placement = new NativeMethods.WINDOWPLACEMENT();
            NativeMethods.GetWindowPlacement(windowHandle, out placement);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8))
                {
                    serializer.Serialize(xmlTextWriter, placement);
                    byte[] xmlBytes = memoryStream.ToArray();
                    return encoding.GetString(xmlBytes);
                }
            }
        }

        /// <summary>
        /// Use to set the user's window properties at startup.  Call at the SourceInitialized event as follows:
        /// 
        /// protected override void OnSourceInitialized(EventArgs e)
        /// {
        ///     base.OnSourceInitialized(e);
        ///     this.SetPlacement(Settings.Default.MainWindowPlacement);
        /// }
        /// </summary>
        /// <param name="window">The main window.</param>
        /// <param name="placementXml">The serialized string containing the settings.</param>
        public static void SetPlacement(this Window window, string placementXml)
        {
            WindowPlacement.SetPlacement(new WindowInteropHelper(window).Handle, placementXml);
        }

        /// <summary>
        /// Use to save the user's window properties.  Call at the Window.Closing event as follows:
        /// 
        /// private void Window_Closing(object sender, CancelEventArgs e)
        /// {
        ///     Settings.Default.MainWindowPlacement = this.GetPlacement();
        ///     Settings.Default.Save();
        /// }
        /// </summary>
        /// <param name="window">The main window.</param>
        public static string GetPlacement(this Window window)
        {
            return WindowPlacement.GetPlacement(new WindowInteropHelper(window).Handle);
        }
    }
}