using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using AngleSharp;
using AngleSharp.Dom;
using LarchSys.Bot.Annotations;
using IWin32Window = System.Windows.Forms.IWin32Window;
using MessageBox = System.Windows.MessageBox;


namespace LarchSys.Bot {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class HeroldWindow : Window, INotifyPropertyChanged, IWin32Window {
        private ObservableCollection<SearchResult> _results;
        private ObservableCollection<string> _searchedLinks;
        private string _searchText;
        private int _progress;
        private int _pageCount;
        private string _status;
        private int _resultsCount;

        public HeroldWindow()
        {
            InitializeComponent();
            Title = "BOT - herold.at Firmensuche";
            DataContext = this;

            BtnSearch.Click += async (s, e) => await Search_OnClick(s, e);
            TxbSearch.KeyDown += async (s, e) => {
                if (e.Key == Key.Enter) await Search_OnClick(s, e);
            };

            Clear();
        }

        private void Clear()
        {
            SearchedLinks = new ObservableCollection<string>();
            Results = new ObservableCollection<SearchResult>();
            Status = "--";
            PageCount = 0;
            Progress = 0;
            ResultsCount = 0;
            SearchText = "";
            BtnSearch.IsEnabled = true;
            BtnExport.IsEnabled = false;
            BtnReset.IsEnabled = false;
        }


        public ObservableCollection<string> SearchedLinks {
            get => _searchedLinks;
            set {
                _searchedLinks = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<SearchResult> Results {
            get => _results;
            set {
                _results = value;
                OnPropertyChanged();
            }
        }
        public string SearchText {
            get => _searchText;
            set {
                _searchText = value;
                OnPropertyChanged();
            }
        }
        public int Progress {
            get => _progress;
            set {
                _progress = value;
                OnPropertyChanged();
            }
        }
        public int PageCount {
            get => _pageCount;
            set {
                _pageCount = value;
                OnPropertyChanged();
            }
        }
        public string Status {
            get => _status;
            set {
                _status = value;
                OnPropertyChanged();
            }
        }
        public int ResultsCount {
            get => _resultsCount;
            set {
                _resultsCount = value;
                OnPropertyChanged();
            }
        }


        private async Task Search_OnClick(object sender, RoutedEventArgs e)
        {
            var text = SearchText;
            if (string.IsNullOrEmpty(text)) {
                MessageBox.Show("Der Suchtext darf nicht leer sein!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            BtnSearch.IsEnabled = false;
            BtnReset.IsEnabled = false;
            BtnExport.IsEnabled = false;

            Status = "Running";

            await RunSearch(text);

            Status = "Finish";

            BtnExport.IsEnabled = true;
            BtnSearch.IsEnabled = true;
            BtnReset.IsEnabled = true;
        }

        private async Task RunSearch(string text)
        {
            try {
                Progress = 0;
                var niceText = text.ToLower().Replace(" ", "-");
                var url = $"https://www.herold.at/gelbe-seiten/was_{niceText}/";
                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                SearchedLinks.Add(url);
                var document = await context.OpenAsync(url);

                var pageCount = PageCount = int.TryParse(document.QuerySelector(".pagination-result > div.row > div.d-none > span > b:nth-child(2)")?.TextContent, out var p) ? p : 1;

                AddRange(QueryInfos(document));

                for (var i = 2; i <= pageCount; i++) {
                    Progress = (int) ((i / (double) pageCount) * 100d);

                    var pageUrl = url + $"?page={i}";
                    SearchedLinks.Add(pageUrl);
                    var doc = await context.OpenAsync(pageUrl);
                    AddRange(QueryInfos(doc));
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            IEnumerable<SearchResult> QueryInfos(IDocument doc)
            {
                var items = doc.QuerySelectorAll("#result-list-container .result-item");
                foreach (var item in items) {
                    yield return new SearchResult {
                        Url = item.QuerySelector("meta[itemprop=\"url\"]").GetAttribute("content"),
                        Name = item.QuerySelector("[itemprop=\"name\"]").TextContent,
                        Category = item.QuerySelector(".result-item-category").TextContent,
                        Address = item.QuerySelector(".address").TextContent,
                        Tel = item.QuerySelector("a[data-category=\"Telefonnummer_result\"]")?.GetAttribute("href")?.Replace("tel:", ""),
                        Img = item.QuerySelector("[itemprop=\"image\"]")?.GetAttribute("src")
                    };
                }
            }
        }

        private void AddRange(IEnumerable<SearchResult> queryInfos)
        {
            Results = new ObservableCollection<SearchResult>(Results.Concat(queryInfos));
            ResultsCount = Results.Count;
        }


        private void Export_OnClick(object sender, RoutedEventArgs args)
        {
            try {
                using var dialog = new SaveFileDialog {
                    AddExtension = true,
                    AutoUpgradeEnabled = true,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    DefaultExt = ".cvs",
                    FileName = "herold-export.csv",
                };

                if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK) {
                    var file = new FileInfo(dialog.FileName);
                    using var fs = file.OpenWrite();
                    using var sw = new StreamWriter(fs, Encoding.UTF8);

                    sw.WriteLine(Row(
                        "Kategorie", "Name", "Adresse", "Tel", "Url"
                    ));

                    foreach (var result in Results) {
                        sw.WriteLine(Row(
                            result.Category,
                            result.Name,
                            result.Address,
                            Regex.Replace(result.Tel ?? string.Empty, @"^(\+\d{2})?(\d{3})(\d{4})(\d*)", "$1 $2 $3 $4").Trim(),
                            result.Url
                        ));
                    }

                    Status = $"{dialog.FileName} saved successful";
                    MessageBox.Show(Status, "saved", MessageBoxButton.OK);
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            static string Row(params string[] a) => string.Join(";", a.Select(_ => $"\"{_}\""));
        }

        private void Reset_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Es werden alle aktuellen Suchergebnisse unwiderruflich gelöscht!", "Zurücksetzen", MessageBoxButton.OKCancel) == MessageBoxResult.OK) {
                Clear();
            }
        }

        public IntPtr Handle => new WindowInteropHelper(this).Owner;

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
