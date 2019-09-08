using System.Windows;


namespace LarchSys.Bot {
    public partial class MainWindow : Window {
        public MainWindow()
        {
            InitializeComponent();
            Title = "LarchSys - BOT";
        }

        private void GelbeSeiten_OnClick(object sender, RoutedEventArgs e)
        {
            new SpiderWindow(new GelbeSeitenSpider()) {Owner = this}.Show();
        }

        private void Herold_OnClick(object sender, RoutedEventArgs e)
        {
            new SpiderWindow(new HeroldSpider()) {Owner = this}.Show();
        }
    }
}
