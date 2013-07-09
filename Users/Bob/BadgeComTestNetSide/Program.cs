using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cloud.BadgeNET;
using Cloud.Static;
using Cloud;

namespace BadgeComTestNetSide
{
    class Program
    {
        static IconOverlay _iconOverlay = null;
        static CLSyncbox _syncbox = null;

        static void Main(string[] args)
        {
            // Create credentials.
            CLCredentials credentials;
            CLError errorFromCredentialsAllocAndInit = CLCredentials.AllocAndInit("034e14679dfd54823e7d45b1e92dc7c95c57fa2b0430cc87462d82d678e7bec2", "7adb097a532be354c0aeccde9b9a3aaa5a0a8c2a6a9238d03b2dc9966e58086e", out credentials);
            if (errorFromCredentialsAllocAndInit != null)
            {
                return;
            }

            // Create a syncbox.
            CLSyncbox syncbox;
            CLError errorFromSyncboxAllocAndInit = CLSyncbox.AllocAndInit(18, credentials, out syncbox, "C:\\Users\\Robertste\\Cloud");
            if (errorFromSyncboxAllocAndInit != null)
            {
                return;
            }

            // Initialize to IconOverlay.
            _iconOverlay = new IconOverlay();
            CLError iconOverlayError = _iconOverlay.Initialize(syncbox.CopiedSettings, syncbox);
            if (iconOverlayError != null)
            {
                return;
            }

            // Did it initialize properly?
            bool fIsInitialized;
            CLError errorFromIsBadgingInitialized = _iconOverlay.IsBadgingInitialized(out fIsInitialized);
            if (errorFromIsBadgingInitialized != null)
            {
                return;
            }

            // Shut down badging when the test exits.
            CLError errorFromShutdown = _iconOverlay.Shutdown();
            if (errorFromShutdown != null)
            {
                return;
            }
        }
    }
}
