using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace HrManagement
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, __) => UpdateLines();
            SizeChanged += (_, __) => UpdateLines();

            // На всякий случай — когда пересчитываются размеры самих блоков
            nodeTop.SizeChanged += (_, __) => UpdateLines();
            nodeLeftDept.SizeChanged += (_, __) => UpdateLines();
            nodeRightDept.SizeChanged += (_, __) => UpdateLines();
            nodeContract.SizeChanged += (_, __) => UpdateLines();
            nodeCommon.SizeChanged += (_, __) => UpdateLines();
            nodeLicense.SizeChanged += (_, __) => UpdateLines();
            nodeMarketing.SizeChanged += (_, __) => UpdateLines();
        }

        private void UpdateLines()
        {
            if (canvasOrgChart == null) return;

            // Дороги России -> два департамента (как на макете: диагонали от низа центра)
            SetLine(lineTopToLeft, nodeTop, 0.50, 1.00, nodeLeftDept, 0.50, 0.00);
            SetLine(lineTopToRight, nodeTop, 0.50, 1.00, nodeRightDept, 0.50, 0.00);

            // Адм. департамент -> Договорной отдел (диагональ вниз-влево)
            // Старт ближе к левому низу, чтобы визуально совпало с макетом
            SetLine(lineLeftToContract, nodeLeftDept, 0.15, 1.00, nodeContract, 0.35, 0.00);

            // Адм. департамент -> Общий отдел (диагональ вниз-вправо)
            // Старт ближе к правому низу
            SetLine(lineLeftToCommon, nodeLeftDept, 0.85, 1.00, nodeCommon, 0.20, 0.00);

            // Академия -> Общий отдел (диагональ вниз-влево)

            // Общий отдел -> Лицензионный (диагональ вниз-влево)
            SetLine(lineCommonToContractLeft, nodeCommon, 0.30, 1.00, nodeLicense, 0.50, 0.00);

            // Общий отдел -> Маркетинг (диагональ вниз-вправо)
            SetLine(lineCommonToMarketingRight, nodeCommon, 0.70, 1.00, nodeMarketing, 0.50, 0.00);
        }

        private void SetLine(Line line,
                             FrameworkElement from, double fromRelX, double fromRelY,
                             FrameworkElement to, double toRelX, double toRelY)
        {
            if (line == null || from == null || to == null || canvasOrgChart == null) return;
            if (from.ActualWidth <= 0 || from.ActualHeight <= 0 || to.ActualWidth <= 0 || to.ActualHeight <= 0) return;

            var p1 = from.TranslatePoint(
                new Point(from.ActualWidth * fromRelX, from.ActualHeight * fromRelY),
                canvasOrgChart);

            var p2 = to.TranslatePoint(
                new Point(to.ActualWidth * toRelX, to.ActualHeight * toRelY),
                canvasOrgChart);

            line.X1 = p1.X; line.Y1 = p1.Y;
            line.X2 = p2.X; line.Y2 = p2.Y;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
