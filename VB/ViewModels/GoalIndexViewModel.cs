using Sustain.Models;

namespace Sustain.ViewModels.GoalVMs
{
    public class GoalIndexViewModel
    {
        public List<Goal> Goals { get; set; } = new();
        public List<EmissionSource> EmissionSources { get; set; } = new();
        public Organization Organization { get; set; }
    }
}
