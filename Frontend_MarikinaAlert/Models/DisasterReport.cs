using System;
using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    public enum ReportCategory { Fire, BuildingCollapse, Medical, Logistics, Infrastructure, Noise }
    public enum ReportPriority { High, Medium, Low, Critical }

    public class DisasterReport
    {
        // LOGIC: Using [Key] and Guid tells the database this is a unique ID.
        // Guid.NewGuid() automatically generates a random ID like 'a1b2-c3d4...'
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // LOGIC: We initialize strings to empty to prevent errors if data is missing.
        public string SenderName { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;

        // This is the raw message from the citizen (e.g., "Tulong sunog!")
        public string RawMessage { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public ReportCategory Category { get; set; }
        public ReportPriority Priority { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Coordinates for the map
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsVerified { get; set; }

        // LOGIC: Tracks which Admin Node created this report. 
        // Essential for syncing data between laptops later.
        public string OriginNodeId { get; set; } = string.Empty;
    }
}