namespace Frontend_MarikinaAlert.Models
{
    public static class Marikina
    {
        public static readonly string[] Barangays =
        [
            "Barangka", "Calumpang", "Concepcion Dos", "Concepcion Uno",
            "Fortune", "Industrial Valley Complex", "Jesus de la Peña", "Malanday",
            "Marikina Heights", "Nangka", "Parang", "San Roque",
            "Santa Elena", "Santo Niño", "Tañong", "Tumana"
        ];

        // Schematic map: one tile per barangay on a 4 x 5 grid, north at the top, west on the left.
        // Positions are approximate (not to scale) and only need to keep neighbours next to each other;
        // correct them here if a barangay looks out of place. Also used to find the closest free team.
        public static readonly IReadOnlyDictionary<string, MapTile> Map = new Dictionary<string, MapTile>
        {
            ["Nangka"] = new(1, 0, "Nangka"),
            ["Parang"] = new(2, 0, "Parang"),
            ["Fortune"] = new(3, 0, "Fortune"),
            ["Malanday"] = new(0, 1, "Malanday"),
            ["Tumana"] = new(1, 1, "Tumana"),
            ["Concepcion Uno"] = new(2, 1, "Conc. Uno"),
            ["Marikina Heights"] = new(3, 1, "M. Heights"),
            ["Jesus de la Peña"] = new(0, 2, "J. dela Peña"),
            ["Santo Niño"] = new(1, 2, "Sto. Niño"),
            ["San Roque"] = new(2, 2, "San Roque"),
            ["Concepcion Dos"] = new(3, 2, "Conc. Dos"),
            ["Tañong"] = new(0, 3, "Tañong"),
            ["Santa Elena"] = new(1, 3, "Sta. Elena"),
            ["Calumpang"] = new(2, 3, "Calumpang"),
            ["Industrial Valley Complex"] = new(0, 4, "IVC"),
            ["Barangka"] = new(1, 4, "Barangka")
        };

        // Rough distance in map tiles; only good for ranking teams, not for travel time.
        public static double TileDistance(string a, string b) =>
            Map.TryGetValue(a, out var p) && Map.TryGetValue(b, out var q)
                ? Math.Sqrt(Math.Pow(p.Col - q.Col, 2) + Math.Pow(p.Row - q.Row, 2))
                : double.MaxValue;

        // Philippines has no DST, so a fixed +8 offset is safe and avoids OS time zone lookups.
        public static DateTime ToLocal(DateTime utc) => utc.AddHours(8);

        public static string TimeAgo(DateTime utc)
        {
            var span = DateTime.UtcNow - utc;
            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return ToLocal(utc).ToString("MMM d, h:mm tt");
        }

        public static string SuggestedAgency(ReportCategory category) => category switch
        {
            ReportCategory.Fire => "BFP Marikina",
            ReportCategory.Medical => "Marikina Rescue 161 / Ambulance",
            ReportCategory.Collapse => "City Engineering / DPWH",
            ReportCategory.Flood => "MCDRRMO",
            _ => "MCDRRMO"
        };

        public static readonly string[] Agencies =
        [
            "BFP Marikina", "Marikina Rescue 161 / Ambulance", "City Engineering / DPWH", "PNP Marikina", "MCDRRMO"
        ];
    }

    public record MapTile(int Col, int Row, string Label);
}
