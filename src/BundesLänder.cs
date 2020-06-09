using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;


namespace LarchSys.Bot {
    public class BundesLänder {
        public Dictionary<string, ZipZone> Map { get; set; }

        public BundesLänder()
        {
            Map = Parse(
                    "Baden-Württemberg",
                    "Bayern",
                    "Berlin",
                    "Brandenburg",
                    "Bremen",
                    "Hamburg",
                    "Hessen",
                    "Mecklenburg-Vorpommern",
                    "Niedersachsen",
                    "Nordrhein-Westfalen",
                    "Rheinland-Pfalz",
                    "Saarland",
                    "Sachsen",
                    "Sachsen-Anhalt",
                    "Schleswig-Holstein",
                    "Thüringen"
                )
                .GroupBy(_ => _.Zip)
                .Select(_ => _.First())
                .ToDictionary(
                    _ => _.Zip,
                    _ => _
                );
        }


        private IEnumerable<ZipZone> Parse(params string[] zones)
        {
            return (
                from zone in zones
                from line in ReadResourceLines($"/Resources/{zone}.csv")
                let parts = line.Split(',')
                where parts.Length == 2
                select new ZipZone {
                    Zone = zone,
                    Zip = parts[0],
                    Community = parts[1],
                }
            );
        }

        private string[] ReadResourceLines(string fileName)
        {
            var type = typeof(App);
            var assembly = type.Assembly;

            var resourceName = $"{type.Namespace}.{fileName.Replace("/", ".").TrimStart('.')}";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var result = reader.ReadToEnd().Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            return result;
        }
    }

    public class ZipZone {
        public string Zone { get; set; }
        public string Zip { get; set; }
        public string Community { get; set; }
    }
}
