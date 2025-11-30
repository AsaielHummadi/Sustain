using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.UserVMs
{
    public class EditProfileViewModel
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

        [StringLength(255, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string? UserPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("UserPassword")]
        public string? UserPasswordConfirmation { get; set; }
    }
}
