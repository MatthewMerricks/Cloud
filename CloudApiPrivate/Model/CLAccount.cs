//
//  CLAccount.cs
//  Cloud Windows
//
//  Created by BobS.
//  Copyright (c) Cloud.com. All rights reserved.

using System;

namespace Cloud.Model
{
    /// <summary>
    /// An object of this class represents a Cloud account.
    /// </summary>
    public class CLAccount
    {
        #region "Properties"

        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; private set; }
        public string Password { get; set; }

        #endregion

        #region "Life Cycle"

        public CLAccount()
        {
            throw new NotSupportedException("Default constructor not supported.");
        }

        public CLAccount(string username, string first, string last, string password)
        {
            UserName = username;
            FirstName = first;
            LastName = last;
            FullName = String.Format("{0} {1}", FirstName, LastName);
            Password = password;
        }
        #endregion
    }
}
