using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    // Order follows a team's normal cycle; OutOfService sits outside it.
    public enum TeamStatus
    {
        Available = 0,
        Dispatched = 1,   // assigned and on the way
        OnScene = 2,
        Returning = 3,    // left the scene, not yet back at base
        OutOfService = 4  // off duty, vehicle down, etc.
    }

    // A unit that can be sent out: an engine, ambulance, rescue boat crew.
    public class ResponseTeam
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Enter the team name."), MaxLength(60)]
        public string Name { get; set; } = "";

        // Agency name as text, matching the Agencies page, so a team can be suggested for the right report type.
        [Required(ErrorMessage = "Choose the agency."), MaxLength(100)]
        public string AgencyName { get; set; } = "";

        // Where the team is stationed. Used to suggest the closest free team.
        [Required(ErrorMessage = "Choose where the team is based.")]
        [MaxLength(60)]
        public string BaseBarangay { get; set; } = "";

        [MaxLength(40)]
        public string? CallSign { get; set; }

        public TeamStatus Status { get; set; }

        // Report the team is working on or coming back from. Cleared when the team is available again.
        public int? AssignedReportId { get; set; }

        public DateTime? StatusChangedAtUtc { get; set; }

        public bool IsBusy => Status is TeamStatus.Dispatched or TeamStatus.OnScene;

        public string StatusText => Status switch
        {
            TeamStatus.OnScene => "On scene",
            TeamStatus.OutOfService => "Out of service",
            _ => Status.ToString()
        };
    }
}
