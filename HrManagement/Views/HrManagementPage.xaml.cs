using HrManagement.Controllers;
using HrManagement.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace HrManagement.Views
{
    public partial class HrManagementPage : Page
    {
        private readonly HrManagementPageController pageController;

        public HrManagementPage()
        {
            InitializeComponent();

            var controller = new HrManagementController(AppData.Db);
            pageController = new HrManagementPageController(controller);
            DataContext = pageController;

            Loaded += (_, __) => UpdateLines();
            SizeChanged += (_, __) => UpdateLines();

            nodeTop.SizeChanged += (_, __) => UpdateLines();
            nodeLeftDept.SizeChanged += (_, __) => UpdateLines();
            nodeRightDept.SizeChanged += (_, __) => UpdateLines();
            nodeContract.SizeChanged += (_, __) => UpdateLines();
            nodeCommon.SizeChanged += (_, __) => UpdateLines();
            nodeLicense.SizeChanged += (_, __) => UpdateLines();
            nodeMarketing.SizeChanged += (_, __) => UpdateLines();
        }

        private void FilterByDepartment_Click(object sender, RoutedEventArgs e)
        {
            var department = (sender as Button)?.Content?.ToString();
            if (department == "Дороги России") department = null;
            pageController.FilterEmployeesByDepartment(department);
        }

        private void RefreshBindings()
        {
            DataContext = null;
            DataContext = pageController;
        }

        private void OpenEmployeeCard_Click(object sender, RoutedEventArgs e)
        {
            pageController.OpenEmployeeCard((sender as FrameworkElement)?.DataContext as EmployeeCardModel);
            RefreshBindings();
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            pageController.AddEmployee();
            RefreshBindings();
        }

        private void StartEditEmployee_Click(object sender, RoutedEventArgs e)
        {
            pageController.StartEditEmployee();
            RefreshBindings();
        }

        private void DismissEmployee_Click(object sender, RoutedEventArgs e)
        {
            pageController.DismissEmployee();
            RefreshBindings();
        }

        private void CloseEmployeeCard_Click(object sender, RoutedEventArgs e)
        {
            pageController.CloseEmployeeCard();
            RefreshBindings();
        }

        private void SaveEmployee_Click(object sender, RoutedEventArgs e)
        {
            pageController.SaveEmployee();
            RefreshBindings();
        }

        private void CancelEditEmployee_Click(object sender, RoutedEventArgs e)
        {
            pageController.CancelEditEmployee();
            RefreshBindings();
        }

        private void UpdateLines()
        {
            if (canvasOrgChart == null) return;
            SetLine(lineTopToLeft, nodeTop, 0.50, 1.00, nodeLeftDept, 0.50, 0.00);
            SetLine(lineTopToRight, nodeTop, 0.50, 1.00, nodeRightDept, 0.50, 0.00);
            SetLine(lineLeftToContract, nodeLeftDept, 0.15, 1.00, nodeContract, 0.35, 0.00);
            SetLine(lineLeftToCommon, nodeLeftDept, 0.35, 1.00, nodeCommon, 0.20, 0.00);
            SetLine(lineCommonToContractLeft, nodeCommon, 0.30, 1.00, nodeLicense, 0.50, 0.00);
            SetLine(lineCommonToMarketingRight, nodeCommon, 0.70, 1.00, nodeMarketing, 0.50, 0.00);
        }

        private void SetLine(Line line, FrameworkElement from, double fromRelX, double fromRelY, FrameworkElement to, double toRelX, double toRelY)
        {
            if (line == null || from == null || to == null || canvasOrgChart == null) return;
            if (from.ActualWidth <= 0 || from.ActualHeight <= 0 || to.ActualWidth <= 0 || to.ActualHeight <= 0) return;
            var p1 = from.TranslatePoint(new Point(from.ActualWidth * fromRelX, from.ActualHeight * fromRelY), canvasOrgChart);
            var p2 = to.TranslatePoint(new Point(to.ActualWidth * toRelX, to.ActualHeight * toRelY), canvasOrgChart);
            line.X1 = p1.X; line.Y1 = p1.Y; line.X2 = p2.X; line.Y2 = p2.Y;
        }
    }
}
