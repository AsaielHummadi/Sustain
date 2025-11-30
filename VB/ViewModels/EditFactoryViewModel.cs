using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.FactoryVMs
{
    public class EditFactoryViewModel
    {
        [Required(ErrorMessage = "Factory name is required")]
        [StringLength(150, ErrorMessage = "Factory name cannot exceed 150 characters")]
        [Display(Name = "Factory Name")]
        public string FactoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Factory code is required")]
        [StringLength(50, ErrorMessage = "Factory code cannot exceed 50 characters")]
        [Display(Name = "Factory Code")]
        public string FactoryCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Factory location is required")]
        [StringLength(150, ErrorMessage = "Factory location cannot exceed 150 characters")]
        [Display(Name = "Factory Location")]
        public string FactoryLocation { get; set; } = string.Empty;
    }
}
