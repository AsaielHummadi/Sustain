using System.ComponentModel.DataAnnotations;

namespace Sustain.ViewModels.GoalVMs
{
    public class UpdateGoalStatusViewModel
    {
        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        public string GoalStatus { get; set; } = string.Empty;
    }
}
