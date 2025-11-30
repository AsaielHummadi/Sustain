using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.UserVMs
{
    public class UpdateUserViewModel
    {
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
        public string UserStatus { get; set; } = "Active";

        [Required]
        public int RoleId { get; set; }

        // Add this property
        public int? FactoryId { get; set; }
    }
}