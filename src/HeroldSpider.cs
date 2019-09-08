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
    public class HeroldSpider : Spider {
        public HeroldSpider()
        {
            Title = "BOT - herold.at Firmensuche";
            ExportFileName = "herold-export.csv";
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


        protected override async Task Search(string text)
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

                AddResults(QueryInfos(document));

                for (var i = 2; i <= pageCount; i++) {
                    Progress = (int) ((i / (double) pageCount) * 100d);

                    var pageUrl = url + $"?page={i}";
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
                var items = doc.QuerySelectorAll("#result-list-container .result-item");

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
        }
    }
}
