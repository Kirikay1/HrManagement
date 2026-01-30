using System.Windows;

namespace HrManagement
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigate(new Views.HrManagementPage());
        }
    }
}
