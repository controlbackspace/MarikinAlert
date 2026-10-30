using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    public class CreateReportViewModel
    {
        [MaxLength(100)]
        [Display(Name = "Your name")]
        public string? SenderName { get; set; }

        // Accepts 09XXXXXXXXX or +639XXXXXXXXX, with optional spaces/dashes.
        [RegularExpression(@"^(\+?63|0)9\d{2}[\s-]?\d{3}[\s-]?\d{4}$",
            ErrorMessage = "Enter a valid mobile number, e.g. 0917 123 4567.")]
        [Display(Name = "Callback number")]
        public string? ContactNumber { get; set; }

        [Required(ErrorMessage = "Choose your barangay so rescuers know where to go.")]
        public string? Barangay { get; set; }

        [MaxLength(200)]
        [Display(Name = "Street / landmark")]
        public string? Landmark { get; set; }

        [Required(ErrorMessage = "Tell us what is happening.")]
        [MinLength(5, ErrorMessage = "Please describe the emergency in a few words.")]
        [MaxLength(1000)]
        [Display(Name = "What is happening?")]
        public string? RawMessage { get; set; }

        // Optional quick-pick from the chips; triage keywords take precedence.
        public ReportCategory? SelectedCategory { get; set; }
    }

    // Dispatcher's "New report" form: the resident form plus how the report came in.
    public class LogReportViewModel : CreateReportViewModel
    {
        [Required(ErrorMessage = "Choose how this report came in.")]
        public ReportSource? Source { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Enter your username.")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class IncidentFeedViewModel
    {
        public IReadOnlyList<DisasterReport> Reports { get; set; } = [];
        public ReportCategory? Category { get; set; }
        public ReportStatus? Status { get; set; }
        public string? Barangay { get; set; }
        public int CriticalCount { get; set; }
        public int OpenCount { get; set; }

        // New reports left unhandled longer than IncidentInsights.OverdueAfter allows.
        public IReadOnlySet<int> OverdueIds { get; set; } = new HashSet<int>();

        // Report id -> id of the earlier report it probably duplicates.
        public IReadOnlyDictionary<int, int> DuplicateOf { get; set; } = new Dictionary<int, int>();

        // Open reports per barangay, busiest first. Always computed from all open reports, ignoring filters.
        public IReadOnlyList<BarangayCount> BarangaySummary { get; set; } = [];
    }

    public record BarangayCount(string Barangay, int Open, int Critical, int Overdue);

    public class ReportStatusViewModel
    {
        public string? Code { get; set; }
        public DisasterReport? Report { get; set; }
        public bool NotFound { get; set; }
    }

    public class HistoryViewModel
    {
        public IReadOnlyList<DisasterReport> Reports { get; set; } = [];
        public ReportStatus? Status { get; set; }
        public string? Search { get; set; }
    }
}
