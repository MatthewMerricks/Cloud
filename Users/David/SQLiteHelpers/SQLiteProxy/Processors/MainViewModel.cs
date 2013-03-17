using Cloud.SQLProxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLiteProxy.Processors
{
    public sealed class MainViewModel
    {
        public static string CheckStatus(string filePath)
        {
            string statusBlockTextToSet;

            if (string.IsNullOrEmpty(filePath))
            {
                statusBlockTextToSet = "FilePath not set";
            }
            else if (File.Exists(filePath))
            {
                try
                {
                    using (ISQLiteConnection encryptedConn = CreateAndOpenCipherconnection(encrypted: true, dbLocation: filePath))
                    {
                        using (ISQLiteCommand getVersionCommand = encryptedConn.CreateCommand())
                        {
                            getVersionCommand.CommandText = "PRAGMA user_version;";
                            int versionNumber = Convert.ToInt32(getVersionCommand.ExecuteScalar());
                            statusBlockTextToSet = "Database is encrypted and is at version " + versionNumber.ToString();
                            return statusBlockTextToSet;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is ISQLiteException)
                        || ((ISQLiteException)ex).ReturnCode != WrappedSQLiteErrorCode.NotADatabase)
                    {
                        statusBlockTextToSet = "Error checking database status: " + ex.Message;
                    }
                }

                try
                {
                    using (ISQLiteConnection unencryptedConn = CreateAndOpenCipherconnection(encrypted: false, dbLocation: filePath))
                    {
                        using (ISQLiteCommand getVersionCommand = unencryptedConn.CreateCommand())
                        {
                            getVersionCommand.CommandText = "PRAGMA user_version;";
                            int versionNumber = Convert.ToInt32(getVersionCommand.ExecuteScalar());
                            statusBlockTextToSet = "Database is not encrypted and is at version " + versionNumber.ToString();
                        }
                    }
                }
                catch (Exception ex)
                {
                    statusBlockTextToSet = "Error checking database status: " + ex.Message;
                }
            }
            else
            {
                statusBlockTextToSet = "File not found at FilePath";
            }

            return statusBlockTextToSet;
        }

        private const string indexDBPassword = "Q29weXJpZ2h0Q2xvdWQuY29tQ3JlYXRlZEJ5RGF2aWRCcnVjaw=="; // <-- if you change this password, you will likely break all clients with older databases
        private static ISQLiteConnection CreateAndOpenCipherconnection(bool encrypted, string dbLocation)
        {
            const string CipherConnectionString = "Data Source=\"{0}\";Pooling=false;Synchronous=Full;UTF8Encoding=True;";

            ISQLiteConnection cipherConn = SQLConstructors.SQLiteConnection(
                string.Format(
                    CipherConnectionString,
                    dbLocation));

            try
            {
                if (encrypted)
                {
                    cipherConn.SetPassword(
                        Encoding.ASCII.GetString(
                            Convert.FromBase64String(indexDBPassword)));
                }

                cipherConn.Open();

                return cipherConn;
            }
            catch
            {
                cipherConn.Dispose();
                throw;
            }
        }

        public static string ChangeDBEncryption(bool encrypt, string filePath)
        {
            string statusBlockTextToSet;

            if (string.IsNullOrEmpty(filePath))
            {
                statusBlockTextToSet = "FilePath not set";
            }
            else if (File.Exists(filePath))
            {
                try
                {
                    using (ISQLiteConnection changeEncryptionConn = CreateAndOpenCipherconnection(encrypted: !encrypt, dbLocation: filePath))
                    {
                        changeEncryptionConn.ChangePassword(
                            (encrypt
                                ? Encoding.ASCII.GetString(
                                    Convert.FromBase64String(indexDBPassword))
                                : null));

                        statusBlockTextToSet = "Database successfully " + (encrypt ? string.Empty : "un") + "encrypted!";
                    }
                }
                catch (Exception ex)
                {
                    statusBlockTextToSet = "Check database status first! Error " + (encrypt ? string.Empty : "un") + "encrypting database: " + ex.Message;
                }
            }
            else
            {
                statusBlockTextToSet = "File not found at FilePath";
            }

            return statusBlockTextToSet;
        }
    }
}