//
//  CLSecureString.cs
//  Cloud Windows
//
//  Created by BobS.
//  Changes from the original code are Copyright (c) Cloud.com. All rights reserved.
//
//  Base code from http://stackoverflow.com/questions/8871337/how-can-i-encrypt-user-settings-such-as-passwords-in-my-application
//
// To encrypt a string:
//      var clearText = "Some string to encrypt"; 
//      var cypherText = CLSecureString.EncryptString(CLSecureString.ToSecureString(clearText)); 
// To decrypt a string:
//      var clearText = CLSecureString.ToInsecureString(CLSecureString.DecryptString(cypherText)); 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;

namespace CloudApiPrivate.Common
{
    public class CLSecureString
    {
        static byte[] entropy = System.Text.Encoding.Unicode.GetBytes("Salt Is Not A Password");

        public static string EncryptString(System.Security.SecureString input)
        {
            byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                System.Text.Encoding.Unicode.GetBytes(ToInsecureString(input)),
                entropy,
                System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedData);
        }

        public static SecureString DecryptString(string encryptedData)
        {
            try
            {
                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedData),
                    entropy,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return ToSecureString(System.Text.Encoding.Unicode.GetString(decryptedData));
            }
            catch
            {
                return new SecureString();
            }
        }

        public static SecureString ToSecureString(string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }

        public static string ToInsecureString(SecureString input)
        {
            string returnValue = string.Empty;
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(input);
            try
            {
                returnValue = System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
            }
            return returnValue;
        }

    }
}
