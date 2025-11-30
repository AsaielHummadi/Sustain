using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.EmissionRecordVMs
{
    public class UpdateEmissionRecordViewModel
    {
        [Required(ErrorMessage = "Please select an emission source")]
        [Display(Name = "Emission Source")]
        public int EmissionSourceId { get; set; }

        [Required(ErrorMessage = "Please select a factory")]
        [Display(Name = "Factory")]
        public int FactoryId { get; set; }

        [Required(ErrorMessage = "Please select the year")]
        [Range(2020, 2030, ErrorMessage = "Year must be between 2020 and 2030")]
        [Display(Name = "Year")]
        public int EmissionYear { get; set; }

        [Required(ErrorMessage = "Please select the month")]
        [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
        [Display(Name = "Month")]
        public int EmissionMonth { get; set; }

        [Required(ErrorMessage = "Please enter the quantity")]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be positive")]
        [Display(Name = "Quantity")]
        public decimal EmissionQuantity { get; set; }
    }
}
