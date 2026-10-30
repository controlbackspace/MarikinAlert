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
}
