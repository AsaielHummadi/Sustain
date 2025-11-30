using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.UserVMs
{
    public class SendInvitationViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string InvitedEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        [Display(Name = "Factory")]
        public int? FactoryId { get; set; }
    }
}
