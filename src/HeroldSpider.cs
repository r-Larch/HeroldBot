﻿using System;
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
    public class HeroldSpider : Spider {
        public IBrowsingContext Browser { get; set; }

        public HeroldSpider()
        {
            Title = "BOT - herold.at Firmensuche";
            ExportFileName = "herold-export.csv";

            var config = Configuration.Default.WithDefaultLoader();
            Browser = BrowsingContext.New(config);
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
            var niceText = searchText.ToLower().Replace(" ", "-");
            var url = $"https://www.herold.at/gelbe-seiten/was_{niceText}/";

            if (pageNum > 1) {
                url += $"?page={pageNum}";
            }

            token.ThrowIfCancellationRequested();

            SearchedLinks.Add(url);
            var document = await Browser.OpenAsync(url, cancellation: token);
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
                    Url = item.QuerySelector("meta[itemprop=\"url\"]")?.GetAttribute("content"),
                    Name = item.QuerySelector("[itemprop=\"name\"]")?.TextContent?.Trim(),
                    Category = item.QuerySelector(".heading")?.TextContent?.Trim(),
                    Address = Address.Parse(item.QuerySelector(".address")?.TextContent),
                    Tel = Regex.Replace(tel, @"^(\+\d{2})?(\d{3})(\d{4})(\d*)", "$1 $2 $3 $4").Trim(),
                    Img = item.QuerySelector("[itemprop=\"image\"]")?.GetAttribute("src"),

                    Website = null,
                    Email = null,
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

            result.Email = Regex.Replace(doc.QuerySelector("#companyBusinessCard .icon-mail")?.NextElementSibling?.GetAttribute("href")?.Trim() ?? string.Empty, @"mailto:([^?]*)\??.*", "$1");
            result.Website = doc.QuerySelector("[data-category=\"Weblink\"]")?.GetAttribute("href");

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
