using System.Windows;


namespace LarchSys.Bot {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow()
        {
            InitializeComponent();
            Title = "LarchSys - BOT";
        }

        private void GelbeSeiten_OnClick(object sender, RoutedEventArgs e)
        {
            new GelbeSeitenWindow().Show();
            Close();
        }

        private void Herold_OnClick(object sender, RoutedEventArgs e)
        {
            new HeroldWindow().Show();
            Close();
        }
    }
}
