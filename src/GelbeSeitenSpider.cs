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
    internal class GelbeSeitenSpider : Spider {
        public IBrowsingContext Browser { get; set; }
        public Task WorkerTask { get; set; }

        public GelbeSeitenSpider()
        {
            Title = "BOT - gelbeseiten.de Firmensuche";
            ExportFileName = "gelbeseiten-export.csv";

            var config = Configuration.Default.WithDefaultLoader();
            Browser = BrowsingContext.New(config);

            WorkerTask = Task.CompletedTask;
        }


        public override Brush Background { get; } = new SolidColorBrush(Color.FromRgb(255, 220, 00));
        public override UIElement Logo { get; } = new Border {
            BorderBrush = new SolidColorBrush(Colors.Black),
            BorderThickness = new Thickness(.34),
            SnapsToDevicePixels = true,
            UseLayoutRounding = false,
            Child = new TextBlock {
                Margin = new Thickness(10),
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Black,
                FontSize = 15,
                Text = "Gelbe Seiten"
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
            var niceText = Uri.EscapeUriString(searchText.ToLower());
            var url = $"https://www.gelbeseiten.de/Suche/{niceText}/Bundesweit";

            if (pageNum > 1) {
                url += $"/Seite-{pageNum}";
            }

            SearchedLinks.Add(url);
            var document = await Browser.OpenAsync(url);
            return document;
        }


        private static int GetPageCount(IDocument listPage)
        {
            var pageCount = 1;
            var paging = Regex.Replace(listPage.QuerySelector(".gs_titel p")?.TextContent ?? string.Empty, @"\s+", " ").Trim();
            if (!string.IsNullOrEmpty(paging)) {
                var match = Regex.Match(paging, @"Treffer (?<from>\d+) - (?<to>\d+) von (?<total>\d+)");
                if (match.Success) {
                    var from = int.Parse(match.Groups["from"].Value) - 1;
                    var to = int.Parse(match.Groups["to"].Value);
                    var total = int.Parse(match.Groups["total"].Value);
                    var pageSize = to - from;
                    pageCount = (int) Math.Ceiling((double) total / pageSize);
                }
            }

            return pageCount;
        }


        private static IEnumerable<SearchResult> GetListItems(IDocument listPage)
        {
            var items = listPage.QuerySelectorAll("#gs_treffer .m08_teilnehmer");
            foreach (var x in items) {
                yield return new SearchResult {
                    Url = x.QuerySelector("[itemprop=\"url\"]")?.GetAttribute("href"),
                    Name = x.QuerySelector("[itemprop=\"name\"]")?.TextContent?.Trim(),
                    Category = x.QuerySelector(".branchen_box span:nth-child(1)")?.TextContent,
                    Address = Regex.Replace(x.QuerySelector("address")?.TextContent ?? string.Empty, @"\s+", " ").Trim(),
                    //Tel = item.QuerySelector(".nummer")?.TextContent,
                    Tel = x.QuerySelector("[itemprop=\"telephone\"]")?.TextContent?.Trim(),
                    Img = x.QuerySelector("[data-lazy-src]")?.GetAttribute("data-lazy-src")
                };
            }
        }


        protected void AddResults(IEnumerable<SearchResult> searchResults)
        {
            var results = searchResults.ToArray();

            QueueDeepScan(results);

            // more efficient then Array.Concat
            var r = new SearchResult[Results.Count + results.Length];
            Results.CopyTo(r, 0);
            results.CopyTo(r, Results.Count);

            Results = new ObservableCollection<SearchResult>(r);
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

            result.Email = doc.QuerySelector("[property=\"email\"]")?.GetAttribute("content");
            result.Website = doc.QuerySelector("[property=\"url\"]")?.GetAttribute("href");

            DeepScanCount++;
            ProgressDeepScan = (int) (((double) DeepScanCount / ResultsCount) * 100d);
        }
    }
}
