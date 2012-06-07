using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.Serialization;
using FileMonitor;

namespace CloudTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // for testing
        private bool fileSelected = false;
        // for testing
        private bool folderSelected = false;
        // for testing
        private MonitorAgent folderMonitor = null;
        // for testing
        private bool folderMonitoringStarted = false;
        // for testing
        private object folderMonitoringStartedLocker = new object();

        // for testing
        private Microsoft.Win32.OpenFileDialog OpenFile
        {
            get
            {
                if (_openFile == null)
                {
                    _openFile = (Microsoft.Win32.OpenFileDialog)this.Resources["OpenFile"];
                }
                return _openFile;
            }
        }
        // for testing
        private Microsoft.Win32.OpenFileDialog _openFile = null;

        // for testing
        private System.Windows.Forms.FolderBrowserDialog OpenFolder
        {
            get
            {
                if (_openFolder == null)
                {
                    _openFolder = (System.Windows.Forms.FolderBrowserDialog)this.Resources["OpenFolder"];
                }
                return _openFolder;
            }
        }
        // for testing
        private System.Windows.Forms.FolderBrowserDialog _openFolder = null;

        public MainWindow()
        {
            // important
            this.Closed += new EventHandler(MainWindow_Closed);

            BadgeNET.IconOverlay.Initialize();

            InitializeComponent();
        }

        // important including everything inside
        /// <summary>
        /// Tie window closed and application unhandled exception to the code inside this eventhandler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_Closed(object sender, EventArgs e)
        {
            BadgeNET.IconOverlay.Shutdown();
        }

        // for testing
        private void OpenFileText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (OpenFile.ShowDialog(this) == true)
            {
                OpenFileText.Content = OpenFile.SafeFileName;
                fileSelected = true;
            }
        }

        // for testing
        private void BadgeFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileSelected)
            {
                BadgeNET.cloudAppIconBadgeType findBadge;
                BadgeNET.IconOverlay.getBadgeTypeForFileAtPath(OpenFile.FileName, out findBadge);// error ignored
                if (findBadge == BadgeNET.cloudAppIconBadgeType.cloudAppBadgeNone)
                {
                    string selectedBadgeTypeString = (string)((ComboBoxItem)BadgeTypeDropDown.SelectedItem).Content;
                    BadgeNET.IconOverlay.setBadgeType(selectedBadgeTypeString == "Syncing"
                        ? BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSyncing
                        : (selectedBadgeTypeString == "Synced"
                            ? BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSynced
                            : (selectedBadgeTypeString == "Selective"
                                ? BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSyncSelective
                                : BadgeNET.cloudAppIconBadgeType.cloudAppBadgeFailed)),
                        OpenFile.FileName);
                    MessageBox.Show("Selected file was badged");
                }
                else
                {
                    BadgeNET.IconOverlay.setBadgeType(null, OpenFile.FileName);
                    MessageBox.Show("Selected file was unbadged");
                }
            }
            else
            {
                MessageBox.Show("Please select a file first");
            }
        }

        // for testing
        private bool rotatingBadgesOnCurrentFile = false;
        // for testing
        private class rotationHolder
        {
            public Action processRotation { get; set; }
        }
        // for testing
        private static rotationHolder currentRotationHolder = new rotationHolder();
        // for testing
        private Timer rotationTimer = new Timer((timerState) =>
        {
            ((rotationHolder)timerState).processRotation();
        },
            currentRotationHolder,
            Timeout.Infinite,
            Timeout.Infinite);
        private Nullable<BadgeNET.cloudAppIconBadgeType> lastRotatedBadgeType = null;

        // for testing
        private void RotateBadgesOnCurrentFile_Click(object sender, RoutedEventArgs e)
        {
            if (fileSelected)
            {
                if (rotatingBadgesOnCurrentFile)
                {
                    rotationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    MessageBox.Show("Stopped rotating badging on current file");
                }
                else
                {
                    MessageBox.Show("Started rotating badging on current file");
                    currentRotationHolder.processRotation = () =>
                    {
                        if (lastRotatedBadgeType == null)
                            lastRotatedBadgeType = BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSyncing;
                        else if (lastRotatedBadgeType.Equals(BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSyncing))
                            lastRotatedBadgeType = BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSynced;
                        else if (lastRotatedBadgeType.Equals(BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSynced))
                            lastRotatedBadgeType = BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSyncSelective;
                        else if (lastRotatedBadgeType.Equals(BadgeNET.cloudAppIconBadgeType.cloudAppBadgeSyncSelective))
                            lastRotatedBadgeType = BadgeNET.cloudAppIconBadgeType.cloudAppBadgeFailed;
                        else if (lastRotatedBadgeType.Equals(BadgeNET.cloudAppIconBadgeType.cloudAppBadgeFailed))
                            lastRotatedBadgeType = null;

                        BadgeNET.IconOverlay.setBadgeType(lastRotatedBadgeType, OpenFile.FileName);
                    };
                    rotationTimer.Change(0, 1000);
                }
                rotatingBadgesOnCurrentFile = !rotatingBadgesOnCurrentFile;
            }
            else
            {
                MessageBox.Show("Please select a file first");
            }
        }

        // for testing
        private void OpenFolderText_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            lock (folderMonitoringStartedLocker)
            {
                if (folderMonitoringStarted)
                {
                    MessageBox.Show("Turn off file monitoring before selecting a different folder");
                }
                else if (OpenFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OpenFolderText.Content = OpenFolder.SelectedPath;
                    folderSelected = true;
                }
            }
        }

        // for testing
        private void MonitorFileSystemButton_Click(object sender, RoutedEventArgs e)
        {
            if (folderSelected)
            {
                lock (folderMonitoringStartedLocker)
                {
                    if (folderMonitoringStarted)
                    {
                        folderMonitor.Dispose();
                        folderMonitor = null;

                        folderMonitoringStarted = false;

                        MessageBox.Show("Folder monitoring stopped");
                    }
                    else
                    {
                        MonitorAgent.CreateNewAndInitialize(OpenFolder.SelectedPath, out folderMonitor, FileChange_OnQueueing, true);
                        MonitorStatus returnStatus;
                        folderMonitor.Start(out returnStatus);

                        folderMonitoringStarted = true;

                        MessageBox.Show("Folder monitoring started");
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a folder first");
            }
        }

        private void FileChange_OnQueueing(MonitorAgent sender, FileChange args)
        {
            lock (queuedChanges)
            {
                queuedChanges.Add(args);
            }
        }
        private List<FileChange> queuedChanges = new List<FileChange>();

        private void LogQueuedChanges()
        {
            foreach (FileChange sender in queuedChanges)
            {
                //<FileChange>
                //  <NewPath>[path for current change]</NewPath>
                //
                //  <!--Only present if the previous path exists:-->
                //  <OldPath>[path for previous location for moves/renames]</OldPath>
                //
                //  <IsFolder>[true for folders, false for files]</IsFolder>
                //  <Type>[type of change that occurred]</Type>
                //  <LastTime>[number of ticks from the DateTime of when the file/folder was last changed]</LastTime>
                //  <CreationTime>[number of ticks from the DateTime of when the file/folder was created]</CreationTime>
                //
                //  <!--Only present if the change has a file size-->
                //  <Size>[hex string of MD5 file checksum]</Size>
                //
                //</FileChange>
                AppendFileChangeProcessedLogXmlString(new XElement("FileChange",
                    new XElement("NewPath", new XText(sender.NewPath)),
                    sender.OldPath == null ? null : new XElement("OldPath", new XText(sender.OldPath)),
                    new XElement("IsFolder", new XText(sender.Metadata.HashableProperties.IsFolder.ToString())),
                    new XElement("Type", new XText(sender.Type.ToString())),
                    new XElement("LastTime", new XText(sender.Metadata.HashableProperties.LastTime.Ticks.ToString())),
                    new XElement("CreationTime", new XText(sender.Metadata.HashableProperties.CreationTime.Ticks.ToString())),
                    sender.Metadata.HashableProperties.Size == null ? null : new XElement("Size", new XText(sender.Metadata.HashableProperties.Size.Value.ToString())),
                    sender.Metadata.MD5 == null ? null : new XElement("MD5", new XText(sender.Metadata.MD5
                        .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                        .Aggregate((previousBytes, newByte) => previousBytes + newByte)))).ToString() + Environment.NewLine);
            }
            File.AppendAllText(testFilePath, "</root>");
        }

        /// <summary>
        /// Path to write processed FileChange log
        /// </summary>
        private const string testFilePath = "C:\\Users\\Public\\Documents\\MonitorAgentOnQueueing.xml";
        /// <summary>
        /// Writes the xml string generated after processing a FileChange event to the log file at 'testFilePath' location
        /// </summary>
        /// <param name="toWrite"></param>
        private static void AppendFileChangeProcessedLogXmlString(string toWrite)
        {
            // If file does not exist, create it with an xml declaration and the beginning of a root tag
            if (!File.Exists(testFilePath))
            {
                File.AppendAllText(testFilePath, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" + Environment.NewLine + "<root>" + Environment.NewLine);
            }
            // Append current xml string to log file
            File.AppendAllText(testFilePath, toWrite);
        }

        // for testing
        private static bool createdFiles = false;
        private static object createdFilesLocker = new object();
        private void CreateTestFiles_Click(object sender, RoutedEventArgs e)
        {
            if (folderMonitoringStarted)
            {
                lock (createdFilesLocker)
                {
                    if (createdFiles)
                    {
                        MessageBox.Show("Created files has already started");
                    }
                    else
                    {
                        createdFiles = true;

                        MessageBox.Show("Creating files started");

                        (new Thread(new ThreadStart(ProcessCreateFiles))).Start();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please start file monitor first");
            }
        }
        private static XmlSerializer FileCreationSerializer
        {
            get
            {
                lock (FileCreationSerializerLocker)
                {
                    if (_fileCreationSerializer == null)
                    {
                        _fileCreationSerializer = new XmlSerializer(typeof(FileCreation.FileCreation));
                    }
                    return _fileCreationSerializer;
                }
            }
        }
        private static XmlSerializer _fileCreationSerializer = null;
        private static object FileCreationSerializerLocker = new object();
        private class RandomHelper
        {
            private Random charRandom = new Random();
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
            public int Next()
            {
                return charRandom.Next(32, 126);
            }
        }
        private static RandomHelper charRandom = new RandomHelper();
        private void ProcessCreateFiles()
        {
            if (!Directory.Exists(OpenFolder.SelectedPath))
            {
                MessageBox.Show("Directory must exist at createFileLocation: " + OpenFolder.SelectedPath);
            }
            else
            {
                FileCreation.FileCreation filesToCreate;
                using (Stream fileStream = new FileStream("TestFileCreations.xml", FileMode.Open))
                {
                    filesToCreate = (FileCreation.FileCreation)FileCreationSerializer.Deserialize(fileStream);
                }
                string createFileLocation = OpenFolder.SelectedPath + "\\";
                foreach (FileCreation.ParallelFileOperationSet currentOperationSet in filesToCreate.Items.OfType<FileCreation.ParallelFileOperationSet>())
                {
                    System.Threading.Tasks.Parallel.ForEach(currentOperationSet.Items,
                        singleOperation =>
                        {
                            FileCreation.SingleFileOperation fileOperation = singleOperation as FileCreation.SingleFileOperation;

                            if (fileOperation == null)
                            {
                                Thread.Sleep((int)singleOperation);
                            }
                            else
                            {
                                string newPath = createFileLocation + fileOperation.NewRelativePath;
                                string oldPath = fileOperation.Type == FileCreation.FileOperationType.Rename ? createFileLocation + fileOperation.OldRelativePath : null;
                                if (fileOperation.IsFolder)
                                {
                                    switch (fileOperation.Type)
                                    {
                                        case FileCreation.FileOperationType.Create:
                                            if (Directory.Exists(newPath))
                                                throw new Exception("Folder already exists at specified path");
                                            Directory.CreateDirectory(newPath);
                                            break;
                                        case FileCreation.FileOperationType.Delete:
                                            if (!Directory.Exists(newPath))
                                                throw new Exception("Folder does not exist at specified path");
                                            Directory.Delete(newPath);
                                            break;
                                        case FileCreation.FileOperationType.Rename:
                                            if (!Directory.Exists(oldPath))
                                                throw new Exception("Folder does not exist at previous location");
                                            if (Directory.Exists(newPath))
                                                throw new Exception("Folder already exists at target location");
                                            Directory.Move(oldPath, newPath);
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (fileOperation.Type)
                                    {
                                        case FileCreation.FileOperationType.Create:
                                            FileInfo createFile = new FileInfo(newPath);
                                            if (!createFile.Directory.Exists)
                                                throw new Exception("Parent directory for file does not exist");
                                            if (createFile.Exists)
                                                throw new Exception("File already exists at target location");
                                            if (string.IsNullOrEmpty(fileOperation.Data.SpecificData))
                                            {
                                                char[] toWrite = new char[fileOperation.Data.SizeDelta];
                                                for (uint i = 0; i < toWrite.LongLength; i++)
                                                {
                                                    toWrite[i] = (char)charRandom.Next();
                                                }

                                                File.AppendAllText(newPath,
                                                    new string(toWrite));
                                            }
                                            else
                                            {
                                                File.AppendAllText(newPath,
                                                    fileOperation.Data.SpecificData);
                                            }
                                            break;
                                        case FileCreation.FileOperationType.Delete:
                                            if (!File.Exists(newPath))
                                                throw new Exception("File does not exist at target location");
                                            File.Delete(newPath);
                                            break;
                                        case FileCreation.FileOperationType.LockedModify:
                                            if (!(new FileInfo(newPath)).Directory.Exists)
                                                throw new Exception("Parent directory for file does not exist");
                                            using (FileStream modifyStream = new FileStream(newPath, FileMode.OpenOrCreate))
                                            {
                                                bool needsWait = fileOperation.MinimumWaitOnLockedModifySpecified
                                                    && fileOperation.MinimumWaitOnLockedModify > 0;
                                                object waitLocker = new object();
                                                if (needsWait)
                                                {
                                                    (new Thread(() =>
                                                    {
                                                        Thread.Sleep(fileOperation.MinimumWaitOnLockedModify);
                                                        lock (waitLocker)
                                                        {
                                                            needsWait = false;
                                                            Monitor.Pulse(waitLocker);
                                                        }
                                                    })).Start();
                                                }
                                                using (StreamWriter modifyWriter = new StreamWriter(modifyStream))
                                                {
                                                    if (string.IsNullOrEmpty(fileOperation.Data.SpecificData))
                                                    {
                                                        char[] toWrite = new char[fileOperation.Data.SizeDelta];
                                                        for (uint i = 0; i < toWrite.LongLength; i++)
                                                        {
                                                            toWrite[i] = (char)charRandom.Next();
                                                        }

                                                        modifyWriter.Write(toWrite);
                                                    }
                                                    else
                                                    {
                                                        modifyWriter.Write(fileOperation.Data.SpecificData);
                                                    }
                                                }
                                                lock (waitLocker)
                                                {
                                                    if (needsWait)
                                                        Monitor.Wait(waitLocker);
                                                }
                                            }
                                            break;
                                        case FileCreation.FileOperationType.Rename:
                                            if (!File.Exists(oldPath))
                                                throw new Exception("File does not exist at previous location");
                                            FileInfo renameToFile = new FileInfo(newPath);
                                            if (renameToFile.Exists)
                                                File.Delete(newPath);
                                            else if (!renameToFile.Directory.Exists)
                                                throw new Exception("Parent directory for target location does not exist");
                                            File.Move(oldPath, newPath);
                                            break;
                                        case FileCreation.FileOperationType.Stream:
                                            if (!(new FileInfo(newPath)).Directory.Exists)
                                                throw new Exception("Parent directory for file does not exist");
                                            bool streamWait = fileOperation.WaitOnStreamCompletionSpecified
                                                && fileOperation.WaitOnStreamCompletion;
                                            object streamWaitLocker = new object();
                                            (new Thread(() =>
                                            {
                                                string toWrite;
                                                if (string.IsNullOrEmpty(fileOperation.Data.SpecificData))
                                                {
                                                    char[] toWriteChars = new char[fileOperation.Data.SizeDelta];
                                                    for (uint i = 0; i < toWriteChars.LongLength; i++)
                                                    {
                                                        toWriteChars[i] = (char)charRandom.Next();
                                                    }

                                                    toWrite = new string(toWriteChars);
                                                }
                                                else
                                                    toWrite = fileOperation.Data.SpecificData;
                                                using (FileStream modifyStream = new FileStream(newPath, FileMode.OpenOrCreate))
                                                {
                                                    using (StreamWriter modifyWriter = new StreamWriter(modifyStream))
                                                    {
                                                        uint streamRepetitionCounter = 0;
                                                        while (streamRepetitionCounter < fileOperation.MillisecondDelayBetweenStreamIterations)
                                                        {
                                                            streamRepetitionCounter++;

                                                            modifyWriter.Write(toWrite);
                                                        }
                                                    }
                                                }

                                                lock (streamWaitLocker)
                                                {
                                                    if (streamWait)
                                                    {
                                                        streamWait = false;
                                                        Monitor.Pulse(streamWaitLocker);
                                                    }
                                                }
                                            })).Start();

                                            lock (streamWaitLocker)
                                            {
                                                if (streamWait)
                                                {
                                                    Monitor.Wait(streamWaitLocker);
                                                }
                                            }
                                            break;
                                        case FileCreation.FileOperationType.UnlockedAppend:
                                            FileInfo appendFile = new FileInfo(newPath);
                                            if (!appendFile.Directory.Exists)
                                                throw new Exception("Parent directory for file does not exist");
                                            using (FileStream modifyStream = new FileStream(newPath, FileMode.OpenOrCreate))
                                            {
                                                using (StreamWriter modifyWriter = new StreamWriter(modifyStream))
                                                {
                                                    if (string.IsNullOrEmpty(fileOperation.Data.SpecificData))
                                                    {
                                                        char[] toWrite = new char[fileOperation.Data.SizeDelta];
                                                        for (uint i = 0; i < toWrite.LongLength; i++)
                                                        {
                                                            toWrite[i] = (char)charRandom.Next();
                                                        }

                                                        modifyWriter.Write(toWrite);
                                                    }
                                                    else
                                                    {
                                                        modifyWriter.Write(fileOperation.Data.SpecificData);
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        });
                }

                lock (createdFilesLocker)
                {
                    createdFiles = false;
                }

                MessageBox.Show("Creating files completed");
            }
        }
        private FolderStructure BuildFolder(string folderPath)
        {
            return BuildFolder(new DirectoryInfo(folderPath));
        }
        private FolderStructure BuildFolder(DirectoryInfo folderInfo)
        {
            FolderStructure toReturn = new FolderStructure()
            {
                Name = folderInfo.Name,
                LastTime = (DateTime.Compare(folderInfo.LastAccessTimeUtc, folderInfo.LastWriteTimeUtc) > 0
                    ? folderInfo.LastAccessTimeUtc
                    : folderInfo.LastWriteTimeUtc).Ticks,
                CreationTime = folderInfo.CreationTimeUtc.Ticks,
            };
            FileInfo[] files = folderInfo.EnumerateFiles().ToArray();
            for (int i = 0; i < files.Length; i++)
            {
                FileStructure toInsert = new FileStructure()
                {
                    Name = files[i].Name,
                    LastTime = (DateTime.Compare(files[i].LastAccessTimeUtc, files[i].LastWriteTimeUtc) > 0
                        ? files[i].LastAccessTimeUtc
                        : files[i].LastWriteTimeUtc).Ticks,
                    CreationTime = files[i].CreationTimeUtc.Ticks,
                    Size = files[i].Length,
                    MD5 = GetMD5(files[i].FullName)
                };
                if (toReturn.Files == null)
                    toReturn.Files = new FileStructure[files.Length];
                ((FileStructure[])toReturn.Files)[i] = toInsert;
            }
            DirectoryInfo[] innerFolders = folderInfo.EnumerateDirectories().ToArray();
            for (int i = 0; i < innerFolders.Length; i++)
            {
                FolderStructure toInsert = BuildFolder(innerFolders[i]);
                if (toReturn.InnerFolders == null)
                    toReturn.InnerFolders = new FolderStructure[innerFolders.Length];
                ((FolderStructure[])toReturn.InnerFolders)[i] = toInsert;
            }
            return toReturn;
        }
        private class FolderStructure
        {
            public string Name { get; set; }
            public long LastTime { get; set; }
            public long CreationTime { get; set; }
            public IEnumerable<FolderStructure> InnerFolders { get; set; }
            public IEnumerable<FileStructure> Files { get; set; }

            public XElement ToElement(string includedRootPath = null)
            {
                return new XElement("Folder",
                    new XElement("Name",
                        new XText((includedRootPath == null ? string.Empty : includedRootPath + "\\") + Name)),
                    new XElement("LastTime",
                        new XText(LastTime.ToString())),
                    new XElement("CreationTime",
                        new XText(CreationTime.ToString())),
                    (InnerFolders == null || InnerFolders.Count() < 1)
                        ? null
                        : new XElement("InnerFolders",
                            InnerFolders.Select(innerFolder => innerFolder.ToElement())),
                    (Files == null || Files.Count() < 1)
                        ? null
                        : new XElement("Files",
                            Files.Select(file => file.ToElement())));
            }
        }
        private class FileStructure
        {
            public string Name { get; set; }
            public long LastTime { get; set; }
            public long CreationTime { get; set; }
            public long Size { get; set; }
            public byte[] MD5 { get; set; }

            public XElement ToElement()
            {
                return new XElement("File",
                    new XElement("Name",
                        new XText(Name)),
                    new XElement("LastTime",
                        new XText(LastTime.ToString())),
                    new XElement("CreationTime",
                        new XText(CreationTime.ToString())),
                    new XElement("Size",
                        new XText(Size.ToString())),
                    new XElement("MD5",
                        new XText(MD5
                            .Select(md5Byte => string.Format("{0:x2}", md5Byte))
                            .Aggregate((previousBytes, newByte) => previousBytes + newByte))));
            }
        }
        /// <summary>
        /// Retrieves MD5 checksum for a given file path
        /// </summary>
        /// <param name="filePath">Location of file to generate checksum</param>
        /// <returns>Returns byte[16] representing the MD5 data</returns>
        private byte[] GetMD5(string filePath)
        {
            // Filestream will fail if reading is blocked or permissions don't allow read,
            // exception will bubble up to CheckMetadataAgainstFile for handling
            using (FileStream mD5Stream = new FileStream(filePath,
                FileMode.Open))
            {
                // compute hash and return using static instance of MD5
                return MD5Hasher.ComputeHash(mD5Stream);
            }
        }
        /// <summary>
        /// Global hashing provider for MD5 checksums
        /// </summary>
        private static MD5 MD5Hasher
        {
            get
            {
                lock (MD5HasherLocker)
                {
                    if (_mD5Hasher == null)
                    {
                        _mD5Hasher = new MD5CryptoServiceProvider();
                    }
                }
                return _mD5Hasher;
            }
        }
        private static MD5 _mD5Hasher;
        private static object MD5HasherLocker = new object();

        private void FinalizeMonitorAgentTestLog_Click(object sender, RoutedEventArgs e)
        {
            if (folderSelected)
            {
                DirectoryInfo savedFolder = new DirectoryInfo(OpenFolder.SelectedPath);
                BuildFolder(savedFolder.FullName).ToElement(savedFolder.FullName).Save("C:\\Users\\Public\\Documents\\TestFileCreationsFinal.xml");

                File.AppendAllText("C:\\Users\\Public\\Documents\\MonitorAgentOutput.xml",
                    "</root>");

                if (folderMonitoringStarted)
                    MonitorFileSystemButton_Click(MonitorFileSystemButton, new RoutedEventArgs(e.RoutedEvent, sender));

                LogQueuedChanges();
                queuedChanges.Clear();

                MessageBox.Show("Logs written to \"C:\\Users\\Public\\Documents\\\"");
            }
            else
            {
                MessageBox.Show("Please select a folder first");
            }
        }

        private void ClearFolderAndLog_Click(object sender, RoutedEventArgs e)
        {
            if (folderSelected)
            {
                if (File.Exists("C:\\Users\\Public\\Documents\\MonitorAgentOutput.xml"))
                    File.Delete("C:\\Users\\Public\\Documents\\MonitorAgentOutput.xml");
                if (File.Exists("C:\\Users\\Public\\Documents\\TestFileCreationsFinal.xml"))
                    File.Delete("C:\\Users\\Public\\Documents\\TestFileCreationsFinal.xml");
                if (File.Exists(testFilePath))
                    File.Delete(testFilePath);
                if (Directory.Exists(OpenFolder.SelectedPath))
                {
                    Directory.Delete(OpenFolder.SelectedPath, true);
                    Directory.CreateDirectory(OpenFolder.SelectedPath);
                }
                MessageBox.Show("Selected folder replaced with an empty one and logs have been purged");
            }
            else
            {
                MessageBox.Show("Please select a folder first");
            }
        }

        private void GenericTests_Click(object sender, RoutedEventArgs e)
        {
            string basePath = "C:\\Users\\Public\\Documents\\CreateFileTests";
            FilePathDictionary<FileMetadata> myData = new FilePathDictionary<FileMetadata>(new DirectoryInfo(basePath),
                RecursiveDeleteCallback,
                null);

            myData.Add(new DirectoryInfo(basePath + "\\A"), new FileMetadata());
            myData.Add(new DirectoryInfo(basePath + "\\A\\B"), new FileMetadata());


            foreach (FilePath currentTest in myData.Keys)
            {
            }

            foreach (FileMetadata currentTest in myData.Values)
            {
            }

            foreach (KeyValuePair<FilePath, FileMetadata> currentTest in myData)
            {

            }

            myData.Add(new DirectoryInfo(basePath + "\\A\\C.txt"), new FileMetadata());
            myData.Add(new DirectoryInfo(basePath + "\\A\\B\\D.txt"), new FileMetadata());
        }

        private void RecursiveDeleteCallback(FilePath sender, FileMetadata args)
        {
        }
    }
}