using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.GoalVMs
{
    public class CreateGoalViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255)]
        [Display(Name = "Title")]
        public string GoalTitle { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? GoalDescription { get; set; }

        [Required(ErrorMessage = "Please select an emission source")]
        [Display(Name = "Emission Source")]
        public int EmissionSourceId { get; set; }

        [Required(ErrorMessage = "Target value is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Target value must be positive")]
        [Display(Name = "Target Value")]
        public decimal GoalGoalValue { get; set; }

        [Required(ErrorMessage = "Period is required")]
        [StringLength(50)]
        [Display(Name = "Period")]
        public string GoalGoalPeriod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        public DateOnly GoalGoalStartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        public DateOnly GoalGoalEndDate { get; set; }
    }
}
