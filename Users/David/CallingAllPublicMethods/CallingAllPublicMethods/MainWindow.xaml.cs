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
            #region debug only code remove this section
            MessageBox.Show("Debug only code: remove this section");
            CallingAllPublicMethods.Models.DesignDependencyObject.SetIsInDesignMode(true);
            CallingAllPublicMethods.ViewModels.MainViewModel mainVM = Application.Current.Resources["MainViewModel"] as CallingAllPublicMethods.ViewModels.MainViewModel;
            mainVM.SyncboxViewModel.SelectedSyncbox = new Models.CLSyncboxProxy(syncbox: null);
            #endregion

            InitializeComponent();
        }
    }
}
