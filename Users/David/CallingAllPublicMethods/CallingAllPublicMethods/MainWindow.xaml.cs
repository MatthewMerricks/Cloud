using CallingAllPublicMethods.Models;
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

namespace CallingAllPublicMethods
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (!DesignDependencyObject.IsInDesignTool
                && MessageBox.Show(
                    "Would you like to disable SSL certificate validation to work with HTTP proxies and debuggers?",
                    "Disable SSL Certificate Validation?",
                    MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                // the following allows use of http-debugging proxies for calls to the Cloud servers
                System.Net.ServicePointManager.ServerCertificateValidationCallback =
                    ((sender, certificate, chain, sslPolicyErrors) => true);
            }

            InitializeComponent();
        }
    }
}