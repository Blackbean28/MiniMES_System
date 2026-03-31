using System.Windows;
using MiniMES_Dashboard.ViewModels; // ViewModel 폴더 참조

namespace MiniMES_Dashboard
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 핵심: 이 화면의 데이터 소스를 우리가 만든 ViewModel로 지정
            this.DataContext = new DashboardViewModel();
        }
    }
}