//
//  MainViewModel.cs
//  Cloud Windows
//
//  Created by DavidBruck.
//  Copyright (c) Cloud.com. All rights reserved.
//

using Cloud.SQLIndexer.SqlModel;
using Cloud.SQLProxies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLiteHelpers.ViewModels
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

        public static string MakeDBAndTest(string filePath, string scriptsFolder)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new NullReferenceException("filePath cannot be null");
                }

                if (string.IsNullOrEmpty(scriptsFolder))
                {
                    throw new NullReferenceException("scriptsFolder cannot be null");
                }

                FileInfo filePathInfo = new FileInfo(filePath);
                DirectoryInfo scriptsFolderInfo = new DirectoryInfo(scriptsFolder);

                if (filePathInfo.Exists)
                {
                    throw new FileExistsException("File at filePath already exists");
                }

                if (!scriptsFolderInfo.Exists)
                {
                    throw new DirectoryNotFoundException("Folder at scriptsFolder does not exist");
                }

                if (!filePathInfo.Directory.Exists)
                {
                    filePathInfo.Directory.Create();
                }

                List<KeyValuePair<int, string>> indexDBScripts = new List<KeyValuePair<int, string>>();

                Encoding ansiEncoding = Encoding.GetEncoding(1252); //ANSI saved from NotePad on a US-EN Windows machine

                foreach (FileInfo currentScript in scriptsFolderInfo.GetFiles())
                {
                    if (currentScript.Name.Length >= 5 // length of 1+-digit number plus ".sql" file extension
                        && currentScript.Name.EndsWith(".sql", StringComparison.InvariantCultureIgnoreCase))
                    {
                        int numChars = 0;
                        for (int numberCharIndex = 0; numberCharIndex < currentScript.Name.Length; numberCharIndex++)
                        {
                            if (!char.IsDigit(currentScript.Name[numberCharIndex]))
                            {
                                numChars = numberCharIndex;
                                break;
                            }
                        }
                        if (numChars > 0)
                        {
                            string nameNumberPortion = currentScript.Name.Substring(0, numChars);
                            int nameNumber;
                            if (int.TryParse(nameNumberPortion, out nameNumber))
                            {
                                using (FileStream scriptStream = currentScript.OpenRead())
                                {
                                    using (StreamReader scriptReader = new StreamReader(scriptStream, ansiEncoding))
                                    {
                                        indexDBScripts.Add(new KeyValuePair<int, string>(nameNumber, scriptReader.ReadToEnd()));
                                    }
                                }
                            }
                        }
                    }
                }

                if (indexDBScripts.Count > 0)
                {
                    using (ISQLiteConnection creationConnection = CreateAndOpenCipherconnection(encrypted: true, dbLocation: filePath))
                    {
                        foreach (string indexDBScript in indexDBScripts.OrderBy(scriptPair => scriptPair.Key).Select(scriptPair => scriptPair.Value))
                        {
                            using (ISQLiteCommand scriptCommand = creationConnection.CreateCommand())
                            {
                                scriptCommand.CommandText = indexDBScript;
                                scriptCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
                else
                {
                    throw new FileNotFoundException("No .sql scripts found in the script folder to execute");
                }
            }
            catch (Exception ex)
            {
                return "Error making db: " + ex.Message;
            }

            string lastTestStarted = "No test started";
            try
            {
                RunTests completedTests = RunTests.None;

                RunTests toRun = RunTests.CreateRootObject | RunTests.TestTriggers;

                if ((toRun & RunTests.CreateRootObject) == RunTests.CreateRootObject)
                {
                    lastTestStarted = "CreateRootObject started";

                    string rootPath = "C:\\Z";

                    using (ISQLiteConnection indexDB = CreateAndOpenCipherconnection(encrypted: true, dbLocation: filePath))
                    {
                        SqlAccessor<FileSystemObject>.InsertRow(
                            indexDB,
                            new FileSystemObject()
                            {
                                EventTimeUTCTicks = 0,
                                IsFolder = true,
                                Name = rootPath,
                                Pending = false
                            });
                    }

                    lastTestStarted = "Verifying CreateRootObject";

                    using (ISQLiteConnection indexDB = CreateAndOpenCipherconnection(encrypted: true, dbLocation: filePath))
                    {
                        FileSystemObject rootObject = SqlAccessor<FileSystemObject>
                            .SelectResultSet(indexDB,
                                "SELECT * FROM FileSystemObjects")
                            .Single();

                        if (rootObject.Name != rootPath
                            || rootObject.ParentFolderId != null
                            || rootObject.CalculatedFullPath != rootPath)
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "The root FileSystemObject does not match on Name, ParentFolderId, and CalculatedFullPath");
                        }
                    }

                    completedTests = completedTests | RunTests.CreateRootObject;
                }

                if ((toRun & RunTests.TestTriggers) == RunTests.TestTriggers)
                {
                    lastTestStarted = "TestTriggers started";

                    using (ISQLiteConnection indexDB = CreateAndOpenCipherconnection(encrypted: true, dbLocation: filePath))
                    {
                        FileSystemObject rootObject = SqlAccessor<FileSystemObject>.SelectResultSet(indexDB,
                            "SELECT * FROM FileSystemObjects WHERE FileSystemObjects.ParentFolderId IS NULL")
                            .Single();

                        FileSystemObject innerFolder = new FileSystemObject()
                            {
                                EventTimeUTCTicks = DateTime.UtcNow.Ticks,
                                IsFolder = true,
                                Name = "A",
                                ParentFolderId = rootObject.FileSystemObjectId,
                                Pending = false
                            };

                        innerFolder.FileSystemObjectId = SqlAccessor<FileSystemObject>.InsertRow<long>(
                            indexDB,
                            innerFolder);

                        FileSystemObject innerFile = new FileSystemObject()
                            {
                                EventTimeUTCTicks = DateTime.UtcNow.Ticks,
                                IsFolder = false,
                                Name = "b.txt",
                                ParentFolderId = innerFolder.FileSystemObjectId,
                                Pending = false
                            };

                        innerFile.FileSystemObjectId = SqlAccessor<FileSystemObject>.InsertRow<long>(
                            indexDB,
                            innerFile);

                        rootObject.Name += "Y";

                        SqlAccessor<FileSystemObject>.UpdateRow(
                            indexDB,
                            rootObject);

                        innerFolder = SqlAccessor<FileSystemObject>
                            .SelectResultSet(indexDB,
                                "SELECT * FROM FileSystemObjects WHERE FileSystemObjectId = " + innerFolder.FileSystemObjectId.ToString())
                            .Single();

                        if (innerFolder.CalculatedFullPath != (rootObject.Name + "\\" + innerFolder.Name))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Updating root folder path did not propagate down at least 1 level deep");
                        }

                        innerFile = SqlAccessor<FileSystemObject>
                            .SelectResultSet(indexDB,
                                "SELECT * FROM FileSystemObjects WHERE FileSystemObjectId = " + innerFile.FileSystemObjectId.ToString())
                            .Single();

                        if (innerFile.CalculatedFullPath != (rootObject.Name + "\\" + innerFolder.Name + "\\" + innerFile.Name))
                        {
                            throw SQLConstructors.SQLiteException(WrappedSQLiteErrorCode.Misuse, "Updating root folder path did not propagate down at least 2 levels deep");
                        }
                    }

                    completedTests = completedTests | RunTests.TestTriggers;
                }

                return (completedTests == RunTests.None
                    ? "Created db successfully, but no tests were set to run"
                    : "Created db successfully, then ran the following tests: " + completedTests.ToString());
            }
            catch (Exception ex)
            {
                return lastTestStarted + ". Error in testing: " + ex.Message;
            }
        }

        [Flags]
        private enum RunTests : int
        {
            None = 0,
            CreateRootObject = 1,
            TestTriggers = 2
        }

        public static string DeleteDB(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new NullReferenceException("filePath cannot be null");
                }

                FileInfo filePathInfo = new FileInfo(filePath);

                if (!filePathInfo.Exists)
                {
                    throw new FileNotFoundException("File at filepath not found");
                }

                filePathInfo.Delete();

                return "Successfully deleted db";
            }
            catch (Exception ex)
            {
                return "Error deleting db: " + ex.Message;
            }
        }
    }

    public sealed class FileExistsException : Exception
    {
        public FileExistsException() : base() { }
        public FileExistsException(string message) : base(message) { }
        public FileExistsException(string message, Exception innerException) : base(message, innerException) { }
    }
}