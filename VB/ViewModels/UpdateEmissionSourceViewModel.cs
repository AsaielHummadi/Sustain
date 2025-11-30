using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.EmissionSourceVMs
{
    public class UpdateEmissionSourceViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(150)]
        [Display(Name = "Name")]
        public string EmissionSourceName { get; set; } = string.Empty;

        [StringLength(255)]
        [Display(Name = "Description")]
        public string? EmissionSourceDescription { get; set; }

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

        [Required(ErrorMessage = "Emission factor is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Emission factor must be positive")]
        [Display(Name = "Emission Factor")]
        public decimal EmissionSourceEmissionFactor { get; set; }

        [StringLength(255)]
        [Display(Name = "Formula")]
        public string? EmissionSourceFormula { get; set; }

        [Required]
        [Display(Name = "Active")]
        public bool EmissionSourceIsActive { get; set; }
    }
}
