using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AngleSharp;
using AngleSharp.Dom;
using MessageBox = System.Windows.MessageBox;


namespace LarchSys.Bot {
    public class HeroldSpider : Spider {
        public IBrowsingContext Browser { get; set; }
        public Task WorkerTask { get; set; }

        public HeroldSpider()
        {
            Title = "BOT - herold.at Firmensuche";
            ExportFileName = "herold-export.csv";

            var config = Configuration.Default.WithDefaultLoader();
            Browser = BrowsingContext.New(config);

            WorkerTask = Task.CompletedTask;
        }


        public override Brush Background { get; } = new SolidColorBrush(Color.FromRgb(255, 238, 0));
        public override UIElement Logo { get; } = new DockPanel {
            Background = new SolidColorBrush(Colors.Black),
            Children = {
                new TextBlock {
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.Black,
                    Margin = new Thickness(10),
                    Text = "HEROLD"
                }
            }
        };


        protected override async Task Search(string search)
        {
            try {
                Progress = 0;

                var document = await GetPage(search);
                AddResults(GetListItems(document));
                PageCount = GetPageCount(document);

                for (var i = 2; i <= PageCount; i++) {
                    Progress = (int) ((i / (double) PageCount) * 100d);

                    document = await GetPage(search, pageNum: i);
                    AddResults(GetListItems(document));
                }

                await WorkerTask;
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task<IDocument> GetPage(string searchText, int pageNum = 1)
        {
            var niceText = searchText.ToLower().Replace(" ", "-");
            var url = $"https://www.herold.at/gelbe-seiten/was_{niceText}/";

            if (pageNum > 1) {
                url += $"?page={pageNum}";
            }

            SearchedLinks.Add(url);
            var document = await Browser.OpenAsync(url);
            return document;
        }


        private static int GetPageCount(IDocument listPage)
        {
            return int.TryParse(listPage.QuerySelector(".pagination-result > div.row > div.d-none > span > b:nth-child(2)")?.TextContent, out var p) ? p : 1;
        }


        private static IEnumerable<SearchResult> GetListItems(IDocument listPage)
        {
            var items = listPage.QuerySelectorAll("#result-list-container .result-item");

            foreach (var item in items) {
                var tel = item.QuerySelector("a[data-category=\"Telefonnummer_result\"]")?.GetAttribute("href")?.Replace("tel:", "") ?? string.Empty;
                yield return new SearchResult {
                    Url = item.QuerySelector("meta[itemprop=\"url\"]").GetAttribute("content"),
                    Name = item.QuerySelector("[itemprop=\"name\"]").TextContent,
                    Category = item.QuerySelector(".result-item-category").TextContent,
                    Address = item.QuerySelector(".address").TextContent,
                    Tel = Regex.Replace(tel, @"^(\+\d{2})?(\d{3})(\d{4})(\d*)", "$1 $2 $3 $4").Trim(),
                    Img = item.QuerySelector("[itemprop=\"image\"]")?.GetAttribute("src")
                };
            }
        }


        protected void AddResults(IEnumerable<SearchResult> searchResults)
        {
            var results = searchResults.ToArray();

            QueueDeepScan(results);

            Results = new ObservableCollection<SearchResult>(Results.Concat(results));
            ResultsCount = Results.Count;

            void QueueDeepScan(params SearchResult[] x)
            {
                WorkerTask = WorkerTask.ContinueWith(_ => Task.WaitAll(x.Select(ScanDetailPage).ToArray()));
            }
        }


        private async Task ScanDetailPage(SearchResult result)
        {
            if (string.IsNullOrEmpty(result.Url)) {
                return;
            }

            var doc = await Browser.OpenAsync(result.Url);

            result.Email = doc.QuerySelector("[itemprop=\"email\"]")?.TextContent;
            result.Website = doc.QuerySelector("[data-category=\"Weblink\"]")?.GetAttribute("href");

            DeepScanCount++;
            ProgressDeepScan = (int) (((double) DeepScanCount / ResultsCount) * 100d);
        }
    }
}
