using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.InvitationVMs
{
    public class EditInvitationViewModel
    {
        [Required]
        public int InvitationId { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters")]
        [Display(Name = "Email Address")]
        public string InvitedEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Factory (for Factory Operators)")]
        public int? FactoryId { get; set; }
    }
}
