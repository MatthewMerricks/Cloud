using CloudApiPublic.Model;
using SyncTestServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace SyncTests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private readonly GenericHolder<ServerService> TestServer = new GenericHolder<ServerService>(null);
        private bool TestServerDisposed = false;
        private ServerData ServerData = null;

        private void StartServer_Click(object sender, RoutedEventArgs e)
        {
            lock (TestServer)
            {
                if (TestServer.Value != null)
                {
                    MessageBox.Show("Error: TestServer already set");
                }
                else
                {
                    try
                    {
                        ServerData = new ServerData();
                        ServerData.InitializeServer(new SyncTestServer.Model.ScenarioServer()
                        {
                            Storage = new SyncTestServer.Model.ServerPhysicalStorageType()
                            {
                                Type = SyncTestServer.Model.StorageType.PhysicalLocation,
                                RootPath = "C:\\SyncServer"
                            },
                            //UserItems = new[]
                            //{
                            //    new SyncTestServer.Model.ServerUserFolder()
                            //    {
                            //        IsFolder = true,
                            //        Uuid = "1",
                            //        Folder = new SyncTestServer.Model.FolderType()
                            //        {
                            //            IsFolder = true,
                            //            RelativePathFromRoot = string.Empty,
                            //            UTCCreatedDateTime = new DateTime(2012, 10, 31) // Happy Halloween!
                            //        }
                            //    }
                            //},
                            Users = new[]
                            {
                                new SyncTestServer.Model.ServerUserType()
                                {
                                    CloudFolder = new SyncTestServer.Model.PhysicalLocationCloudFolderType()
                                    {
                                        Type = SyncTestServer.Model.StorageType.PhysicalLocation,
                                        RootPath = string.Empty,
                                        InitialData = new SyncTestServer.Model.DefinedValuesForCloudFolderType()
                                        {
                                            Type = SyncTestServer.Model.InitialCloudFolderDataTypeType.DefinedValues,
                                            PathsWithMetadata = new[]
                                            {
                                                new SyncTestServer.Model.FolderType()
                                                {
                                                    IsFolder = true,
                                                    RelativePathFromRoot = string.Empty,
                                                    UTCCreatedDateTime = new DateTime(2012, 10, 31) // Happy Halloween!
                                                }
                                            }
                                        }
                                    },
                                    UUid = "1",
                                    Devices = new[]
                                    {
                                        new SyncTestServer.Model.ServerDeviceType()
                                        {
                                            AKey = "0000000000000000000000000000000000000000000000000000000000000000",
                                            FriendlyName = "Test",
                                            UDid = Guid.Empty.ToString()
                                        }
                                    },
                                    Username = "Test",
                                    Password = "Test"
                                }
                            }
                        },
                        () =>
                        {
                            MessageBox.Show("Server failed to lock on the account of the user for making metadata changes, simultaneous syncs were processed intertwined");
                        });
                        TestServer.Value = ServerService.GetInstance(ServerData);
                        MessageBox.Show("TestServer started");
                    }
                    catch (Exception ex)
                    {
                        if (ex is System.Net.HttpListenerException
                            && ex.Message == "The parameter is incorrect")
                        {
                            MessageBox.Show("Error: Probably need to use netsh or httpcfg to register the URI:" + Environment.NewLine +
                                "http://msdn.microsoft.com/en-us/library/ms733768.aspx" + Environment.NewLine +
                                "See where this MessageBox text is located in code for comment on the commands I used");
                            // Here is the comment I mention in the MessageBox text above:
                            // (first make sure you redirect the urls to your local machine which will break normal cloud function: edit %systemroot%\System32\drivers\etc\hosts by adding 127.0.0.1 {remote url here} lines for all the domains)
                            // (also, make sure that World Wide Web Publishing Service Windows Service is stopped and set to manual which is also known as the IIS Service but NOT the IIS Admin Service which you can keep running)
                            // netsh http add urlacl url=http://upd-edge.cloudburrito.com:80/get_file/ user={domain}\{user}
                            // netsh http add urlacl url=http://upd-edge.cloudburrito.com:80/put_file/ user={domain}\{user}
                            // netsh http add urlacl url=http://push-edge.cloudburrito.com:80/events/ user={domain}\{user}
                            // netsh http add urlacl url=http://auth-edge.cloudburrito.com:80/device/ user={domain}\{user}
                            // netsh http add urlacl url=http://auth-edge.cloudburrito.com:80/user/ user={domain}\{user}
                            // netsh http add urlacl url=http://mds-edge.cloudburrito.com:80/private/ user={domain}\{user}
                            // netsh http add urlacl url=http://mds-edge.cloudburrito.com:80/sync/ user={domain}\{user}
                        }
                        else
                        {
                            MessageBox.Show("Error: An error occurred starting TestServer: " + ex.Message);
                        }
                    }
                }
            }
        }

        private void RunTestClient_Click(object sender, RoutedEventArgs e)
        {
            lock (TestServer)
            {
                if (TestServerDisposed)
                {
                    MessageBox.Show("Error: TestServer disposed");
                }
                else if (TestServer.Value == null)
                {
                    MessageBox.Show("Error: TestServer not started");
                }
                else
                {
                    MessageBox.Show("TestClient started");

                    CloudApiPublic.SyncBox testBox = new CloudApiPublic.SyncBox();
                    testBox.Start(SyncImplementations.SyncSettings.Instance);

                    //CloudApiPublic.Sync.SyncEngine testEngine = new CloudApiPublic.Sync.SyncEngine(SyncImplementations.SyncData.Instance,
                    //    SyncImplementations.SyncSettings.Instance);
                    //testEngine.Run(true);
                }
            }
        }

        private void DisposeServer_Click(object sender, RoutedEventArgs e)
        {
            lock (TestServer)
            {
                if (TestServerDisposed)
                {
                    MessageBox.Show("Error: TestServer already disposed");
                }
                else if (TestServer.Value == null)
                {
                    MessageBox.Show("Error: TestServer never started");
                }
                else
                {
                    TestServer.Value.Dispose();
                    TestServerDisposed = true;
                    MessageBox.Show("TestServer disposed");
                }
            }
        }
    }
}