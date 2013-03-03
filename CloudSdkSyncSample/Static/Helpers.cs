/****************************** Module Header ******************************\
 Module Name:  Helpers.cs
 Portions Copyright (c) Microsoft Corporation.
 
 The P/Invoke signature some native Windows APIs used by the code sample.
 
 This source is subject to the Microsoft Public License.
 See http://www.microsoft.com/en-us/openness/resources/licenses.aspx#MPL
 All other rights reserved.
 
 THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
 EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED 
 WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
\***************************************************************************/
// The CreateMediumIntegrityProcess() method from http://code.msdn.microsoft.com/windowsdesktop/CSCreateLowIntegrityProcess-5dbb7cb9/sourcecode?fileId=21657&pathId=1953311234, with modifications.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Security.Principal;
using Cloud.Support;
using Cloud.Model;

namespace SampleLiveSync.Static
{
    public static class Helpers
    {
        private static CLTrace _trace = CLTrace.Instance;

        /// <summary>
        /// The function launches an application at medium integrity level. 
        /// </summary>
        /// <param name="commandLine">
        /// The command line to be executed. The maximum length of this string is 32K 
        /// characters. 
        /// </param>
        /// <remarks>
        /// To start a medium-integrity process, 
        /// 1) Duplicate the handle of the current process, which is at medium 
        ///    integrity level.
        /// 2) Use SetTokenInformation to set the integrity level in the access token 
        ///    to Medium.
        /// 3) Use CreateProcessAsUser to create a new process using the handle to 
        ///    the medium integrity access token.
        /// </remarks>
        internal static void CreateMediumIntegrityProcess(string commandLine, NativeMethod.CreateProcessFlags creationFlags)
        {
            NativeMethod.SafeTokenHandle hToken = null;
            NativeMethod.SafeTokenHandle hNewToken = null;
            IntPtr pIntegritySid = IntPtr.Zero;
            int cbTokenInfo = 0;
            IntPtr pTokenInfo = IntPtr.Zero;
            NativeMethod.STARTUPINFO si = new NativeMethod.STARTUPINFO();
            NativeMethod.PROCESS_INFORMATION pi = new NativeMethod.PROCESS_INFORMATION();

            try
            {
                // Open the primary access token of the process.
                if (!NativeMethod.OpenProcessToken(Process.GetCurrentProcess().Handle,
                    NativeMethod.TOKEN_DUPLICATE | NativeMethod.TOKEN_ADJUST_DEFAULT |
                    NativeMethod.TOKEN_QUERY | NativeMethod.TOKEN_ASSIGN_PRIMARY,
                    out hToken))
                {
                    throw new Win32Exception();
                }

                // Duplicate the primary token of the current process.
                if (!NativeMethod.DuplicateTokenEx(hToken, 0, IntPtr.Zero,
                    NativeMethod.SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    NativeMethod.TOKEN_TYPE.TokenPrimary, out hNewToken))
                {
                    throw new Win32Exception();
                }

                // Create the low integrity SID.
                if (!NativeMethod.AllocateAndInitializeSid(
                    ref NativeMethod.SECURITY_MANDATORY_LABEL_AUTHORITY, 1,
                    NativeMethod.SECURITY_MANDATORY_MEDIUM_RID,
                    0, 0, 0, 0, 0, 0, 0, out pIntegritySid))
                {
                    throw new Win32Exception();
                }

                NativeMethod.TOKEN_MANDATORY_LABEL tml;
                tml.Label.Attributes = NativeMethod.SE_GROUP_INTEGRITY;
                tml.Label.Sid = pIntegritySid;

                // Marshal the TOKEN_MANDATORY_LABEL struct to the native memory.
                cbTokenInfo = Marshal.SizeOf(tml);
                pTokenInfo = Marshal.AllocHGlobal(cbTokenInfo);
                Marshal.StructureToPtr(tml, pTokenInfo, false);

                // Set the integrity level in the access token to low.
                if (!NativeMethod.SetTokenInformation(hNewToken,
                    NativeMethod.TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, pTokenInfo,
                    cbTokenInfo + NativeMethod.GetLengthSid(pIntegritySid)))
                {
                    throw new Win32Exception();
                }

                // Create the new process at the Low integrity level.
                si.cb = Marshal.SizeOf(si);
                if (!NativeMethod.CreateProcessAsUser(hNewToken, null, commandLine,
                    IntPtr.Zero, IntPtr.Zero, false, (uint)creationFlags, IntPtr.Zero, null, ref si,
                    out pi))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                // Centralized cleanup for all allocated resources. 
                if (hToken != null)
                {
                    hToken.Close();
                    hToken = null;
                }
                if (hNewToken != null)
                {
                    hNewToken.Close();
                    hNewToken = null;
                }
                if (pIntegritySid != IntPtr.Zero)
                {
                    NativeMethod.FreeSid(pIntegritySid);
                    pIntegritySid = IntPtr.Zero;
                }
                if (pTokenInfo != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pTokenInfo);
                    pTokenInfo = IntPtr.Zero;
                    cbTokenInfo = 0;
                }
                if (pi.hProcess != IntPtr.Zero)
                {
                    NativeMethod.CloseHandle(pi.hProcess);
                    pi.hProcess = IntPtr.Zero;
                }
                if (pi.hThread != IntPtr.Zero)
                {
                    NativeMethod.CloseHandle(pi.hThread);
                    pi.hThread = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Determine whether the current process has administrative privileges.
        /// </summary>
        /// <returns>bool: true: Is in the Administrator group.</returns>
        internal static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null)
                {
                    throw new InvalidOperationException("Couldn't get the current user identity");
                }
                var principal = new WindowsPrincipal(identity);

                // Check if this user has the Administrator role. If they do, return immediately.
                // If UAC is on, and the process is not elevated, then this will actually return false.
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    _trace.writeToLog(9, "Helpers: IsAdministrator: IsInRole adminstrator.  Return true.");
                    return true;
                }

                // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
                if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
                {
                    // Operating system does not support UAC; skipping elevation check.
                    _trace.writeToLog(9, "Helpers: IsAdministrator: OS does not support UAC.  Return falsee.");
                    return false;
                }

                int tokenInfLength = Marshal.SizeOf(typeof(int));
                IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

                try
                {
                    var token = identity.Token;
                    var result = SampleLiveSync.Static.NativeMethod.GetTokenInformation(token, SampleLiveSync.Static.NativeMethod.TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                    if (!result)
                    {
                        var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                        throw new InvalidOperationException("Couldn't get token information", exception);
                    }

                    var elevationType = (SampleLiveSync.Static.NativeMethod.TokenElevationType)Marshal.ReadInt32(tokenInformation);

                    switch (elevationType)
                    {
                        case SampleLiveSync.Static.NativeMethod.TokenElevationType.TokenElevationTypeDefault:
                            // TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: User is not using a split token, so they cannot elevate.  Return false.");
                            return false;
                        case SampleLiveSync.Static.NativeMethod.TokenElevationType.TokenElevationTypeFull:
                            // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: User has a split token, and the process is running elevated. Assuming they're an administrator. Return true.");
                            return true;
                        case SampleLiveSync.Static.NativeMethod.TokenElevationType.TokenElevationTypeLimited:
                            // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: IsInRole User has a split token, but the process is not running elevated. Return false.");
                            return false;
                        default:
                            // Unknown token elevation type.
                            _trace.writeToLog(9, "Helpers: IsAdministrator: Unknown token elevation type.  Return false.");
                            return false;
                    }
                }
                finally
                {
                    if (tokenInformation != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tokenInformation);
                    }
                }
            }
            catch (Exception ex)
            {
                CLError error = ex;
                error.LogErrors(_trace.TraceLocation, _trace.LogErrors);
                _trace.writeToLog(1, "Helpers: IsAdministrator: ERROR: Exception: Msg: <{0}>. Return false.", ex.Message);
                return false;
            }
        }
    }
}
