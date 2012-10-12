using System.Windows;

namespace TestAppWPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            automaticUpdater2.MenuItem = mnuCheckForUpdates;
        }

        private void automaticUpdater2_BeforeInstalling(object sender, wyDay.Controls.BeforeArgs e)
        {
            //e.Cancel = true;
        }
    }
}