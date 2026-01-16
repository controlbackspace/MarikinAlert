using System.ComponentModel.DataAnnotations;

namespace Frontend_MarikinaAlert.Models
{
    public class AdminUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        // Identifies which laptop this admin is assigned to (e.g., "Barangay_Hall_PC")
        public string AssignedNodeName { get; set; } = string.Empty;
    }
}