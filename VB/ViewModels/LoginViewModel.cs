using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.AuthVMs
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Email")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string UserPassword { get; set; } = string.Empty;
    }
}
