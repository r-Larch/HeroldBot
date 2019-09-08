using System;
using System.Collections.Generic;
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
        public GelbeSeitenSpider()
        {
            Title = "BOT - gelbeseiten.de Firmensuche";
            ExportFileName = "gelbeseiten-export.csv";
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


        protected override async Task Search(string text)
        {
            try {
                Progress = 0;
                var niceText = Uri.EscapeUriString(text.ToLower());
                var url = $"https://www.gelbeseiten.de/Suche/{niceText}/Bundesweit";
                var config = Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                SearchedLinks.Add(url);
                var document = await context.OpenAsync(url);

                var pageCount = 1;
                var paging = Regex.Replace(document.QuerySelector(".gs_titel p")?.TextContent ?? string.Empty, @"\s+", " ").Trim();
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

                PageCount = pageCount;

                AddResults(QueryInfos(document));

                for (var i = 2; i <= pageCount; i++) {
                    Progress = (int) ((i / (double) pageCount) * 100d);

                    var pageUrl = url + $"/Seite-{i}";
                    SearchedLinks.Add(pageUrl);
                    var doc = await context.OpenAsync(pageUrl);
                    AddResults(QueryInfos(doc));
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.ToString(), e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }

            IEnumerable<SearchResult> QueryInfos(IDocument doc)
            {
                var items = doc.QuerySelectorAll("#gs_treffer .m08_teilnehmer");
                foreach (var item in items) {
                    yield return new SearchResult {
                        Url = item.QuerySelector("meta[itemprop=\"url\"]")?.GetAttribute("href"),
                        Name = item.QuerySelector("[itemprop=\"name\"]")?.TextContent?.Trim(),
                        Category = item.QuerySelector(".branchen_box span:nth-child(1)")?.TextContent,
                        Address = Regex.Replace(item.QuerySelector("address")?.TextContent ?? string.Empty, @"\s+", " ").Trim(),
                        //Tel = item.QuerySelector(".nummer")?.TextContent,
                        Tel = item.QuerySelector("[itemprop=\"telephone\"]")?.TextContent?.Trim(),
                        Img = item.QuerySelector("[data-lazy-src]")?.GetAttribute("data-lazy-src")
                    };
                }
            }
        }
    }
}
