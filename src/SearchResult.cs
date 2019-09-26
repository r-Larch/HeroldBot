using System.Text.RegularExpressions;


namespace LarchSys.Bot {
    public class SearchResult {
        public string Name { get; set; }
        public Address Address { get; set; }
        public string Tel { get; set; }
        public string Url { get; set; }
        public string Category { get; set; }
        public string Img { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
    }

    public class Address {
        public string StreatLine { get; set; }
        public string ZipLine { get; set; }

        private static Regex _regex;
        private static Regex Regex => _regex ??= new Regex(@"(\d{4}\d*.*)$", RegexOptions.Multiline | RegexOptions.Compiled);

        public static Address Parse(string address)
        {
            if (string.IsNullOrEmpty(address)) {
                return new Address();
            }

            var match = Regex.Match(address);
            if (match.Success) {
                return new Address {
                    StreatLine = address.Substring(0, match.Groups[1].Index),
                    ZipLine = match.Groups[1].Value
                };
            }

            return new Address {
                StreatLine = address,
            };
        }
    }
}
