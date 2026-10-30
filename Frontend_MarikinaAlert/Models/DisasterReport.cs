using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    public enum ReportCategory
    {
        Unclassified = 0,
        Fire = 1,
        Collapse = 2,
        Medical = 3,
        Flood = 4
    }

    // Order matters: higher value = more urgent, used for sorting the dashboard.
    public enum ReportPriority
    {
        Normal = 0,
        High = 1,
        Critical = 2
    }

    public enum ReportStatus
    {
        Active = 0,     // just came in, waiting for a dispatcher
        Verified = 1,   // dispatcher confirmed it is legitimate (IsVerified protocol)
        Relayed = 2,    // forwarded to an agency
        Resolved = 3,   // closed, moves to History
        Dismissed = 4   // archived as noise / false alarm, moves to History
    }

    public class DisasterReport
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string? SenderName { get; set; }

        [MaxLength(20)]
        public string? ContactNumber { get; set; }

        [Required, MaxLength(60)]
        public string Barangay { get; set; } = "";

        [MaxLength(200)]
        public string? Landmark { get; set; }

        [Required, MaxLength(1000)]
        public string RawMessage { get; set; } = "";

        public ReportCategory Category { get; set; }
        public ReportPriority Priority { get; set; }
        public ReportStatus Status { get; set; }

        // Keywords the triage engine matched, shown to dispatchers so they can see why it was flagged.
        [MaxLength(200)]
        public string? MatchedKeywords { get; set; }

        public bool IsVerified { get; set; }

        [MaxLength(100)]
        public string? RelayedTo { get; set; }

        // Random code the resident uses to check their report's status. Ids are sequential and guessable; this isn't.
        [MaxLength(12)]
        public string? TrackingCode { get; set; }

        public string? TrackingCodeDisplay => TrackingCode is { Length: 8 } c ? $"{c[..4]}-{c[4..]}" : TrackingCode;

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public string LocationText =>
            string.IsNullOrWhiteSpace(Landmark) ? $"Brgy. {Barangay}" : $"{Landmark}, Brgy. {Barangay}";

        public bool IsOpen => Status is ReportStatus.Active or ReportStatus.Verified or ReportStatus.Relayed;
    }
}
