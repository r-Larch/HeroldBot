using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AngleSharp;
using AngleSharp.Dom;
using MessageBox = System.Windows.MessageBox;


namespace LarchSys.Bot {
    internal class GelbeSeitenSpider : Spider {
        private readonly BundesLänder _bundesLänder;
        public IBrowsingContext Browser { get; set; }

        public GelbeSeitenSpider()
        {
            Title = "BOT - gelbeseiten.de Firmensuche";
            ExportFileName = "gelbeseiten-export.csv";

            _bundesLänder = new BundesLänder();

            var config = Configuration.Default.WithDefaultLoader();
            Browser = BrowsingContext.New(config);
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


        protected override async Task Search(string search, CancellationToken token)
        {
            try {
                Progress = 0;

                var document = await GetPage(search, token: token);
                AddResults(GetListItems(document).ToArray());
                PageCount = GetPageCount(document);

                var page = 1;

                foreach (var batch in Enumerable.Range(2, PageCount - 1).Batch(20)) {
                    token.ThrowIfCancellationRequested();
                    await Task.WhenAll(
                        batch.Select(async pageNum => {
                                var document = await GetPage(search, pageNum, token);
                                AddResults(GetListItems(document).ToArray());

                                Interlocked.Increment(ref page);
                                Progress = (int) ((page / (double) PageCount) * 100d);
                            }
                        )
                    );
                }

                foreach (var batch in Results.Batch(20)) {
                    token.ThrowIfCancellationRequested();
                    await Task.WhenAll(batch.Select(_ => ScanDetailPage(_, token)));
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task<IDocument> GetPage(string searchText, int pageNum = 1, CancellationToken token = default)
        {
            var niceText = Uri.EscapeUriString(searchText.ToLower());
            var url = $"https://www.gelbeseiten.de/Suche/{niceText}/Bundesweit";

            if (pageNum > 1) {
                url += $"/Seite-{pageNum}";
            }

            token.ThrowIfCancellationRequested();

            SearchedLinks.Add(url);
            var document = await Browser.OpenAsync(url, cancellation: token);
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


        private async Task ScanDetailPage(SearchResult result, CancellationToken token)
        {
            if (string.IsNullOrEmpty(result.Url)) {
                return;
            }

            token.ThrowIfCancellationRequested();

            var doc = await Browser.OpenAsync(result.Url, cancellation: token);

            result.Facebook = doc.QuerySelector(".icon-social_facebook")?.ParentElement?.GetAttribute("href");

            DeepScanCount++;
            ProgressDeepScan = (int) (((double) DeepScanCount / ResultsCount) * 100d);
        }


        protected void AddResults(SearchResult[] results)
        {
            // more efficient then Array.Concat
            var r = new SearchResult[Results.Count + results.Length];
            Results.CopyTo(r, 0);
            results.CopyTo(r, Results.Count);

            Results = new ObservableCollection<SearchResult>(r);
            ResultsCount = Results.Count;
        }
    }
}
