using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.SubscriptionVMs
{
    public class ProcessSubscriptionViewModel
    {
        [Required]
        [StringLength(150)]
        public string OrganizationName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string OrganizationIndustry { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string OrganizationCity { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserFname { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserLname { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string UserEmail { get; set; } = string.Empty;

        [StringLength(20)]
        public string? UserPhone { get; set; }

        [Required]
        [StringLength(255, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string UserPassword { get; set; } = string.Empty;

        [Required]
        [Compare("UserPassword")]
        [DataType(DataType.Password)]
        public string UserPasswordConfirmation { get; set; } = string.Empty;

        [Required]
        public int SubscriptionPlanId { get; set; }
    }
}
