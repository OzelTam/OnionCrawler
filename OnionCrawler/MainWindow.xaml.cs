using OnionCrawler.Lib;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OnionCrawler.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            QueuedUrls.ItemsSource = viewModel.queuedLinks;
            LinksInProgress.ItemsSource = viewModel.processingLinks;
            FetchedWebpages.ItemsSource = viewModel.fetchedPages;
        }



    }
}