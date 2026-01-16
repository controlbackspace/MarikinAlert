using System;
using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    public enum ReportCategory { Fire, BuildingCollapse, Medical, Logistics, Infrastructure, Noise }
    public enum ReportPriority { High, Medium, Low, Critical }

    // NEW: The Status Enum
    public enum ReportStatus { Active, OnGoing, Resolved }

    public class DisasterReport
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // --- RESTORED PROPERTIES (Essential for Build) ---
        public string SenderName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string RawMessage { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public ReportCategory Category { get; set; }
        public ReportPriority Priority { get; set; }

        // NEW: The Status Property
        public ReportStatus Status { get; set; } = ReportStatus.Active;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVerified { get; set; }

        public string OriginNodeId { get; set; } = string.Empty;
    }
}