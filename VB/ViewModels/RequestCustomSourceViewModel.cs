using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.EmissionSourceVMs
{
    public class RequestCustomSourceViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(150)]
        [Display(Name = "Name")]
        public string EmissionSourceName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(255)]
        [Display(Name = "Description")]
        public string EmissionSourceDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Period is required")]
        [StringLength(50)]
        [Display(Name = "Period")]
        public string EmissionSourcePeriod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Scope is required")]
        [StringLength(50)]
        [Display(Name = "Scope")]
        public string EmissionSourceScope { get; set; } = string.Empty;

        [Required(ErrorMessage = "Unit is required")]
        [StringLength(50)]
        [Display(Name = "Unit")]
        public string EmissionSourceUnit { get; set; } = string.Empty;

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(500)]
        [Display(Name = "Purpose")]
        public string Purpose { get; set; } = string.Empty;
    }
}
