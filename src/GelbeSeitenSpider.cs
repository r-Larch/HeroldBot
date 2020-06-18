using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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
        private BundesLänder _bundesLänder;
        public IBrowsingContext Browser { get; set; }
        public Task WorkerTask { get; set; }

        public GelbeSeitenSpider()
        {
            Title = "BOT - gelbeseiten.de Firmensuche";
            ExportFileName = "gelbeseiten-export.csv";

            _bundesLänder = new BundesLänder();

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
            var from = (int.TryParse(listPage.QuerySelector("#loadMoreStartIndex")?.TextContent, out var f) ? f : 0) - 1;
            var to = int.TryParse(listPage.QuerySelector("#loadMoreGezeigteAnzahl")?.TextContent, out var t) ? t : 0;
            var total = int.TryParse(listPage.QuerySelector("#loadMoreGesamtzahl")?.TextContent, out var a) ? a : 0;

            var pageSize = to - from;
            var pageCount = (int) Math.Ceiling((double) total / pageSize);

            return pageCount;
        }


        private IEnumerable<SearchResult> GetListItems(IDocument listPage)
        {
            var items = listPage.QuerySelectorAll("#gs_treffer .mod-Treffer");

            foreach (var x in items) {
                var address = Address.Parse(Regex.Replace(x.QuerySelector("[data-wipe-name=\"Adresse\"]")?.TextContent ?? string.Empty, @"\s+", " ").Trim());
                address.Update(_bundesLänder.Map);

                var id = x.QuerySelector("[data-realid]")?.GetAttribute("data-realid");

                yield return new SearchResult {
                    Url = $"https://www.gelbeseiten.de/gsbiz/{id}",
                    Name = x.QuerySelector("[data-wipe-name]")?.TextContent?.Trim(),
                    Category = x.QuerySelector(".mod-Treffer--besteBranche")?.TextContent?.Trim(),
                    Address = address,
                    Tel = x.QuerySelector("[data-wipe-name=\"Kontaktdaten\"]")?.TextContent?.Trim(),
                    Img = x.QuerySelector("[data-lazy-src]")?.GetAttribute("data-lazy-src"),

                    Website = x.QuerySelector(".contains-icon-homepage")?.GetAttribute("href")?.Trim(),
                    Email = Regex.Replace(x.QuerySelector(".contains-icon-email")?.GetAttribute("href")?.Trim() ?? string.Empty, @"mailto:([^?]*)\??.*", "$1")
                };
            }
        }


        protected void AddResults(IEnumerable<SearchResult> searchResults)
        {
            var results = searchResults.ToArray();

            // QueueDeepScan(results);

            // more efficient then Array.Concat
            var r = new SearchResult[Results.Count + results.Length];
            Results.CopyTo(r, 0);
            results.CopyTo(r, Results.Count);

            Results = new ObservableCollection<SearchResult>(r);
            ResultsCount = Results.Count;

            //void QueueDeepScan(params SearchResult[] x)
            //{
            //    WorkerTask = WorkerTask.ContinueWith(_ => Task.WaitAll(x.Select(ScanDetailPage).ToArray()));
            //}
        }


        //private async Task ScanDetailPage(SearchResult result)
        //{
        //    if (string.IsNullOrEmpty(result.Url)) {
        //        return;
        //    }

        //    var doc = await Browser.OpenAsync(result.Url);

        //    result.Email = doc.QuerySelector("[property=\"email\"]")?.GetAttribute("content");
        //    result.Website = doc.QuerySelector("[property=\"url\"]")?.GetAttribute("href");

        //    DeepScanCount++;
        //    ProgressDeepScan = (int) (((double) DeepScanCount / ResultsCount) * 100d);
        //}
    }
}
