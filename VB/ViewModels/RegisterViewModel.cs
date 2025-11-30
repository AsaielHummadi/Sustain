using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.AuthVMs
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Organization name is required")]
        [StringLength(150)]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Industry is required")]
        [StringLength(100)]
        [Display(Name = "Industry")]
        public string OrganizationIndustry { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string OrganizationCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string UserFname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string UserLname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(150)]
        [Display(Name = "Email")]
        public string UserEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? UserPhone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string UserPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("UserPassword", ErrorMessage = "Passwords do not match")]
        public string UserPasswordConfirmation { get; set; } = string.Empty;

        [Required]
        public int SubscriptionPlanId { get; set; }
    }
}
