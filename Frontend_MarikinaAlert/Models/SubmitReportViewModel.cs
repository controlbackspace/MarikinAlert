using System.ComponentModel.DataAnnotations;

namespace MarikinAlert.Web.Models
{
    public class SubmitReportViewModel
    {
        // Encapsulation: Grouping related data together
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a phone number")]
        [Phone]
        public string ContactNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "We need to know where you are")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe the emergency")]
        public string RawMessage { get; set; } = string.Empty;
    }
}