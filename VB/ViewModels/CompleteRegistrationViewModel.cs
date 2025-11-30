using Sustain.ViewModels.Validation;
using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.InvitationVMs
{
    public class CompleteRegistrationViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [Display(Name = "First Name")]
        public string UserFname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [Display(Name = "Last Name")]
        public string UserLname { get; set; } = string.Empty;

        [PhoneNumber(ErrorMessage = "Please enter a valid phone number (e.g., +966566193395 or 0566193395)")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string? UserPhone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string UserPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("UserPassword", ErrorMessage = "Password and confirmation password do not match")]
        public string UserPasswordConfirmation { get; set; } = string.Empty;
    }
}
