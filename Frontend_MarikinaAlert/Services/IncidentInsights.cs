using Frontend_MarikinaAlert.Models;

namespace Frontend_MarikinaAlert.Services
{
    /// <summary>Dispatcher hints computed from the open reports: possible duplicates and overdue reports.</summary>
    public static class IncidentInsights
    {
        // Reports of the same category from the same barangay this close together are likely the same incident.
        public static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(15);

        // How long a report may stay "New" before it is flagged as waiting too long.
        public static TimeSpan? OverdueAfter(ReportPriority priority) => priority switch
        {
            ReportPriority.Critical => TimeSpan.FromMinutes(5),
            ReportPriority.High => TimeSpan.FromMinutes(10),
            _ => null
        };

        /// <summary>
        /// Free teams for a report, best first: teams of the agency that should respond, then the closest on the map.
        /// </summary>
        public static IReadOnlyList<ResponseTeam> RankTeams(IEnumerable<ResponseTeam> teams, DisasterReport report, string agencyName) =>
            teams
                .Where(t => t.Status == TeamStatus.Available)
                .OrderBy(t => string.Equals(t.AgencyName, agencyName, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(t => Marikina.TileDistance(t.BaseBarangay, report.Barangay))
                .ThenBy(t => t.Name)
                .ToList();

        public static bool IsOverdue(DisasterReport report, DateTime nowUtc) =>
            report.Status == ReportStatus.Active
            && OverdueAfter(report.Priority) is { } limit
            && nowUtc - report.CreatedAtUtc >= limit;

        /// <summary>
        /// Maps each later report to the earliest report it probably duplicates.
        /// Reports chain: if B is within the window of A and C within the window of B, both point to A.
        /// Unclassified reports are skipped because "same category" means nothing for them.
        /// </summary>
        public static IReadOnlyDictionary<int, int> FindDuplicates(IEnumerable<DisasterReport> reports)
        {
            var result = new Dictionary<int, int>();

            var groups = reports
                .Where(r => r.Category != ReportCategory.Unclassified)
                .GroupBy(r => (Barangay: r.Barangay.Trim().ToLowerInvariant(), r.Category));

            foreach (var group in groups)
            {
                DisasterReport? original = null;
                DisasterReport? previous = null;

                foreach (var report in group.OrderBy(r => r.CreatedAtUtc))
                {
                    if (previous is not null && report.CreatedAtUtc - previous.CreatedAtUtc <= DuplicateWindow)
                    {
                        result[report.Id] = original!.Id;
                    }
                    else
                    {
                        original = report;
                    }
                    previous = report;
                }
            }

            return result;
        }
    }
}
